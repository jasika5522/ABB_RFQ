# Project walkthrough — ABB Supplier RFQ and Bidding Portal

Interview-oriented guide to **demonstrate** the application and explain how it is built.

## Table of contents

1. [Application overview](#a-application-overview)
2. [Role-by-role walkthrough](#b-role-by-role-walkthrough)
3. [End-to-end demonstration](#c-end-to-end-demonstration)
4. [Tie scenario](#c2-tie-scenario-alternate-path)
5. [Important technical explanations](#d-important-technical-explanations)
6. [Important API examples](#e-important-api-examples)
7. [Interview discussion points](#f-interview-discussion-points)

---

## A. Application overview

The portal digitizes a procurement cycle: publish an RFQ with mandatory line items, run time-boxed bidding rounds, rank submitted bids by total price, obtain approver sign-off (or reject ties), and record a single award—all with JWT-secured APIs, an Angular role-based UI, and PostgreSQL audit history.

**URLs (local):**

| Component | URL |
|-----------|-----|
| Angular | `http://localhost:4200` |
| API + Swagger | `http://localhost:5013`, `http://localhost:5013/swagger` |

---

## B. Role-by-role walkthrough

### Supplier

| Goal | Angular route | API |
|------|---------------|-----|
| Register / login | `/register`, `/login` | `POST /api/auth/register`, `POST /api/auth/login` |
| Dashboard | `/supplier/dashboard` | — |
| Profile | `/supplier/profile` | `GET/PUT /api/suppliers/me` |
| Open RFQs | `/supplier/rfqs`, `/supplier/rfqs/:rfqId` | `GET /api/supplier/rfqs*` |
| Submit bid | From RFQ detail | `POST /api/rfqs/{rfqId}/rounds/{roundId}/bids` |
| My bids | `/supplier/my-bids` | `GET /api/bids/me`, `GET /api/bids/{bidId}` |
| Withdraw | Bid detail | `PATCH /api/bids/{bidId}/withdraw` |

### Procurement Officer

| Goal | Angular route | API |
|------|---------------|-----|
| Dashboard | `/procurement/dashboard` | — |
| Suppliers | `/procurement/suppliers` | `GET/PATCH /api/suppliers*` |
| RFQs | `/procurement/rfqs`, create, detail | `POST/PUT/GET /api/rfqs*` |
| Rounds | RFQ detail (publish/close) | `/api/rfqs/{id}/rounds*` |
| Evaluate | `/procurement/evaluations` | `POST .../evaluate`, `GET .../evaluation` |
| Audit | `/audit-logs` | `GET /api/audit-logs*` |

### Approver

| Goal | Angular route | API |
|------|---------------|-----|
| Dashboard | `/approver/dashboard` | — |
| Evaluations | `/approver/evaluations` | `GET .../evaluation`, `POST .../evaluation/decision` |
| Awards | `/approver/awards` | `POST .../award`, `GET /api/awards` |
| Suppliers (read-only) | `/approver/suppliers` | `GET /api/suppliers*` |
| Audit | `/audit-logs` | `GET /api/audit-logs*` |

---

## C. End-to-end demonstration

Use Swagger or the Angular UI. Suggested order:

1. **Register a supplier** — `/register` or `POST /api/auth/register` (status `Pending`).
2. **Log in as Procurement Officer** — `po.m12@example.com` / `Password123!` (local dev only).
3. **Approve supplier** — `/procurement/suppliers/{id}` → Approve, or `PATCH /api/suppliers/{id}/status` with `{ "approvalStatus": "Approved" }`.
4. **Create RFQ** — `/procurement/rfqs/create` with at least two line items (all mandatory for bidders later).
5. **Create Round 1** — On RFQ detail, set a **future** deadline → `POST /api/rfqs/{rfqId}/rounds`.
6. **Publish round** — `PATCH .../rounds/{roundId}/publish` (RFQ becomes visible to suppliers).
7. **Log in as Supplier** — approved account from step 1.
8. **View RFQ** — `/supplier/rfqs/:rfqId`; confirm eligibility shows published round.
9. **Submit bid** — Quote every line with unit price &gt; 0.
10. **Second supplier** — Register another supplier, get approval, log in, submit a **different** total.
11. **Close round** — PO: `PATCH .../rounds/{roundId}/close`.
12. **Evaluate** — PO: `/procurement/evaluations/{rfqId}/{roundId}` → Evaluate, or `POST .../evaluate`.
13. **Log in as Approver** — `approver.m12@example.com` / `Password123!`.
14. **Review ranking** — `/approver/evaluations/{rfqId}/{roundId}`; if unique lowest rank, **Approve** evaluation.
15. **Award** — `/approver/awards/{rfqId}` with winning `bidId` and award comment.
16. **Audit** — `/audit-logs` — show `BidSubmitted`, `RfqRoundEvaluated`, `EvaluationApproved`, `RfqAwarded`.

---

## C2. Tie scenario (alternate path)

1. Two suppliers submit bids whose **totals match** (same rank 1).
2. PO closes and **evaluates** — response indicates tied lowest rank.
3. Approver opens evaluation detail and chooses **Reject** with a **mandatory reason**:

   ```json
   { "decision": "Rejected", "reason": "Lowest rank is tied; new round required." }
   ```

4. PO creates **Round 2** on the same RFQ (RFQ/round status rules require prior round **Evaluated**).
5. PO publishes Round 2; suppliers submit **new** bids.
6. Repeat close → evaluate → approver **approve** (unique lowest) → **award**.

The system never auto-awards the second-ranked supplier when rank 1 is tied.

---

## D. Important technical explanations

### JWT authentication flow

1. Client sends credentials to `POST /api/auth/login`.
2. API verifies BCrypt hash, issues JWT signed with `Jwt:Key` (issuer/audience/expiry configured).
3. Angular stores the token and attaches `Authorization: Bearer` on each request (interceptor).
4. `[Authorize]` attributes enforce role; missing/invalid token → **401**; wrong role → **403**.

### Role-based authorization

Coarse control at the controller (`Supplier`, `ProcurementOfficer`, `Approver`). Supplier bid endpoints are entirely role-gated. Evaluation endpoints exclude suppliers. Approver-only: evaluation decision and award POST.

### Dapper repository pattern

Interfaces (`IBidRepository`, etc.) keep services testable. Implementations use `DapperContext.CreateConnection()`, `QueryAsync` / `ExecuteAsync`, and explicit SQL strings—no EF `DbContext`.

### Database transaction during bid submission

Single transaction: validate supplier approval and round state → insert bid → insert all bid lines → audit row → commit. Unique constraint violation rolls back and surfaces **409**.

### Deadline enforcement

`BidDeadlineRules.IsBiddingOpen`: strictly `deadline > UtcNow`. Withdraw uses the same check.

### Duplicate bid prevention

Partial unique index on active (`Submitted`) bids per `(rfq_id, round_id, supplier_id)`. Withdrawn bids excluded from the index so a new row can be inserted.

### Ranking function

`get_ranked_bids` aggregates line totals, filters `Submitted`, applies `RANK() OVER (ORDER BY total_price ASC)`. Equal totals → equal rank (e.g. 1, 1, 3).

### Audit logging

Critical mutations write to `audit_log` in the same transaction with JSON old/new snapshots and optional client IP.

---

## E. Important API examples

Representative payloads only—replace IDs and tokens with your environment.

### Supplier registration

```http
POST /api/auth/register
Content-Type: application/json

{
  "fullName": "Alex Supplier",
  "email": "alex.supplier@example.com",
  "password": "Password123!",
  "companyName": "Alex Components Ltd",
  "registrationNumber": "REG-90001",
  "contactPerson": "Alex Supplier",
  "phone": "+15550001111"
}
```

### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "po.m12@example.com",
  "password": "Password123!"
}
```

Response includes `token`, `expiresAt`, and `user` (no password hash).

### RFQ creation (Procurement Officer)

```http
POST /api/rfqs
Authorization: Bearer <po-token>
Content-Type: application/json

{
  "rfqNumber": "RFQ-DEMO-001",
  "title": "Control Panel Components",
  "description": "Hackathon demo RFQ",
  "currencyCode": "USD",
  "items": [
    { "itemName": "Circuit Breaker 32A", "quantity": 10, "unit": "EA" },
    { "itemName": "Terminal Block", "quantity": 50, "unit": "EA" }
  ]
}
```

### Create and publish round

```http
POST /api/rfqs/1/rounds
Authorization: Bearer <po-token>
Content-Type: application/json

{ "deadline": "2026-12-31T18:00:00Z" }
```

```http
PATCH /api/rfqs/1/rounds/1/publish
Authorization: Bearer <po-token>
```

### Bid submission (Supplier)

```http
POST /api/rfqs/1/rounds/1/bids
Authorization: Bearer <supplier-token>
Content-Type: application/json

{
  "items": [
    { "rfqItemId": 1, "unitPrice": 125.50 },
    { "rfqItemId": 2, "unitPrice": 4.25 }
  ]
}
```

### Evaluation (Procurement Officer)

```http
POST /api/rfqs/1/rounds/1/evaluate
Authorization: Bearer <po-token>
```

### Approver decision (unique lowest)

```http
POST /api/rfqs/1/rounds/1/evaluation/decision
Authorization: Bearer <approver-token>
Content-Type: application/json

{
  "decision": "Approved"
}
```

### Award (Approver)

```http
POST /api/rfqs/1/rounds/1/award
Authorization: Bearer <approver-token>
Content-Type: application/json

{
  "bidId": 42,
  "awardComment": "Lowest unique evaluated bid per approved evaluation."
}
```

More examples: `RFQAPI/RFQAPI/RFQAPI.http`.

---

## F. Interview discussion points

| Question | Short answer |
|----------|----------------|
| Why Dapper instead of EF Core? | Required by spec; SQL visibility for PostgreSQL indexes, composite FKs, and `get_ranked_bids`. |
| Why PostgreSQL? | Strong constraints, partial unique indexes, window functions for ranking, JSONB audit payloads. |
| How is bid submission atomic? | Single transaction: bid + items + audit; rollback on failure or duplicate. |
| How are concurrent duplicate bids prevented? | Partial unique index + transaction; second insert → **409**. |
| How is the deadline enforced? | `BidDeadlineRules` strict future check before insert/withdraw. |
| How are bid amounts protected? | No PO bid API pre-evaluation; supplier queries scoped by `supplier_id`; evaluation after close only. |
| How are ties handled? | `RANK()` shares rank 1; approver **rejects** with reason; new round—no rank-2 auto award. |
| How is the winner selected? | Approver explicitly passes `bidId` on award after **EvaluationApproved** and rank-1 uniqueness checks. |
| How would you scale? | Stateless API behind load balancer, connection pooling, read replicas for audit/reporting, async notifications. |
| Production improvements? | Vault secrets, integration tests, notifications, MFA, observability, E2E tests. |
| Cursor AI usage? | Assisted coding/docs/tests; validated via builds, 14 xUnit tests, manual E2E, Swagger—not blindly trusted. |

---

**Related docs:** [README.md](README.md) (full reference), [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md) (architecture rationale), [database/README.md](database/README.md) (schema detail).