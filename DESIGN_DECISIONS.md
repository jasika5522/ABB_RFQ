# Design decisions — ABB Supplier RFQ and Bidding Portal

Concise technical rationale aligned with the **implemented** codebase (suitable for interview discussion).

---

## 1. Architecture and layered design

We use **Controller → Service → Repository → PostgreSQL** with an Angular SPA as the client. Controllers stay thin (HTTP, auth, status codes). Services own validation and workflow rules. Repositories own SQL, transactions, and audit writes. This matches the hackathon requirement for clear separation without introducing CQRS, MediatR, or microservices.

## 2. Why Dapper instead of Entity Framework Core

The problem statement required **Dapper**. Explicit SQL makes it easier to review:

- Partial unique index for one active bid per round
- Composite foreign keys tying bids to `(round_id, rfq_id)`
- The `get_ranked_bids()` function using `RANK()`

EF Core would add migration overhead for a time-boxed prototype without improving those PostgreSQL-specific features.

## 3. PostgreSQL schema and relational constraints

Ten tables model users, suppliers, RFQ headers/items/rounds, bids, evaluations, a single award per RFQ, and audit history. **CHECK** constraints encode role and status enums. **Composite unique keys** on `rfq_rounds` and `bids` ensure child rows cannot reference mismatched RFQ/round combinations—important when line items and bid items both carry `rfq_id`.

## 4. Repository and service responsibilities

**Repositories** open connections via `DapperContext`, use parameterized commands, and wrap multi-step writes in transactions. **Services** translate DTOs, call repositories, and throw `ApiException` with intentional HTTP status codes (e.g. **404** for “bid not found” instead of leaking another supplier’s data).

## 5. JWT authentication and RBAC

Login returns a JWT with `sub`, `user_id`, email, and **role** claim. `[Authorize(Roles = …)]` on controllers enforces coarse-grained access. Fine-grained rules (e.g. supplier can only read own bids) are enforced in repository `WHERE supplier_id = @SupplierId` clauses.

## 6. Supplier bid confidentiality

There is **no** procurement endpoint to list competitor bids or unit prices before evaluation. Suppliers use `/api/bids/*` only. Procurement and approver price visibility begins after **evaluate**, via evaluation DTOs backed by `bid_evaluations`. The Angular UI does not substitute for this—API design enforces confidentiality.

## 7. Transactional bid submission

`BidRepository.SubmitAsync`:

1. `BEGIN` transaction, lock supplier and round rows
2. Verify approval, published RFQ/round, deadline
3. Insert `bids` and all `bid_items`
4. Insert `audit_log` (`BidSubmitted`)
5. `COMMIT` (or rollback on any failure / unique violation)

Bid and audit succeed or fail together.

## 8. Deadline and duplicate bid prevention

**Deadline:** `BidDeadlineRules.IsBiddingOpen(deadline, utcNow)` implements strict `deadline > now` (submission at the exact deadline is rejected).

**Duplicates:** Application checks plus `ux_active_supplier_rfq_round` (partial index excluding `Withdrawn`). A second active insert raises `23505`, mapped to **409** *"You already have an active bid for this round."* Withdrawal removes the row from the index scope so a **new** bid row can be inserted.

## 9. Bid evaluation and `RANK()` calculation

Evaluation runs only when RFQ and round are **Closed**. The repository calls `get_ranked_bids`, which sums `quantity × unit_price` for **Submitted** bids only, then applies `RANK() OVER (ORDER BY total_price ASC)`. Results are copied into `bid_evaluations` and RFQ/round statuses move to **Evaluated**.

## 10. Equal lowest bid and tie resolution

Tied rank-1 totals set `isLowestRankTied` in the API response. The approver must **reject** with a reason (stored via audit / decision workflow). The system does **not** award rank 2. Procurement then creates the next `round_number` per Module 4 rules.

## 11. Approval versus award

| Concept | Storage | Effect |
|---------|---------|--------|
| **Evaluation decision** | `audit_log` actions `EvaluationApproved` / `EvaluationRejected` | Gates whether award is allowed |
| **Award** | `awards` table + `RfqAwarded` audit | Single winner per RFQ; RFQ → **Awarded** |

Approval confirms the evaluation outcome; award is a separate, explicit `bidId` + mandatory `awardComment`.

## 12. Audit logging

`AuditLogHelper` inserts JSON snapshots (`old_values` / `new_values`) in the same transaction as supplier status changes, RFQ writes, bid submit/withdraw, evaluate, decisions, and awards. Read APIs expose history to PO and Approver only.

## 13. Important assumptions

- One **currency** per RFQ (`currency_code` on header).
- All RFQ lines are **mandatory** in bids.
- PO and Approver users are **provisioned in the database**, not via public registration.
- Server clock (`DateTimeOffset.UtcNow`) is authoritative for deadline checks in the API layer.

## 14. Potential production improvements

- Secrets in a vault; remove credentials from any committed config.
- Integration tests with ephemeral PostgreSQL (Testcontainers) for constraints and transactions.
- Optional `IClock` abstraction if DB and app clocks must align to the millisecond.
- Notification service for publish/award events; optional OTP/MFA at login.
- Caching or materialized views if evaluation read traffic grows; read replicas for audit reporting.
- Full approver RFQ UI instead of placeholder route; Playwright/Cypress regression suite.