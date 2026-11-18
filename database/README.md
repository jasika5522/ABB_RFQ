# ABB Supplier RFQ and Bidding Portal – Database

PostgreSQL schema for the RFQ and bidding workflow. Scripts in this folder are the **source of truth** for table structure. Execute them on the host where PostgreSQL runs (not necessarily the API development machine).

## Overview

The database supports:

- User accounts with three roles (Supplier, ProcurementOfficer, Approver)
- Supplier registration and approval
- RFQs with line items and multiple bidding rounds
- Supplier bids (submit, withdraw, resubmit within a round)
- Evaluation with ranked totals
- A single final award per RFQ
- Audit trail for important actions

## Tables (10)

| # | Table | Purpose |
|---|--------|---------|
| 1 | `users` | Login identity, role, password hash, active flag |
| 2 | `suppliers` | Company profile linked 1:1 to a Supplier user |
| 3 | `rfqs` | Request for quotation header (currency, status, owner) |
| 4 | `rfq_items` | Line items (quantity, unit) to be quoted |
| 5 | `rfq_rounds` | Bidding rounds per RFQ (deadline, publish/close lifecycle) |
| 6 | `bids` | Supplier bid per round (Submitted / Withdrawn / Accepted / Rejected) |
| 7 | `bid_items` | Unit price per RFQ item within a bid |
| 8 | `bid_evaluations` | Persisted total price and rank after evaluation |
| 9 | `awards` | One winning award per RFQ |
| 10 | `audit_log` | JSON snapshots of changes for auditing |

## ER relationships (text)

```
users (1) ──< (0..1) suppliers
users (1) ──< (*) rfqs [created_by]
users (1) ──< (*) bid_evaluations [evaluated_by]
users (1) ──< (*) awards [awarded_by]
users (1) ──< (*) audit_log [user_id, optional]

rfqs (1) ──< (*) rfq_items
rfqs (1) ──< (*) rfq_rounds
rfqs (1) ── (0..1) awards

rfq_rounds (1) ──< (*) bids
suppliers (1) ──< (*) bids

bids (1) ──< (*) bid_items
rfq_items (1) ──< (*) bid_items [via rfq_item_id + rfq_id]

bids (1) ── (0..1) bid_evaluations
bids (1) ── (0..1) awards [winning bid]
```

## Important keys and foreign keys

- **Composite uniqueness on rounds:** `(round_id, rfq_id)` on `rfq_rounds` so `bids` can reference round and RFQ together.
- **Composite uniqueness on bids:** `(bid_id, rfq_id)` for `bid_items`; `(bid_id, rfq_id, round_id)` for evaluations and awards.
- **Composite uniqueness on items:** `(rfq_item_id, rfq_id)` so bid lines cannot attach items from another RFQ.

## Important constraints and indexes

- **Email:** `ux_users_email` on `LOWER(email)` — case-insensitive unique login.
- **Roles:** `chk_users_role` — Supplier, ProcurementOfficer, Approver only.
- **Supplier approval:** Pending / Approved / Suspended — only Approved suppliers should bid (enforced in API).
- **RFQ currency:** ISO-like 3-letter `currency_code`.
- **One published round:** `ux_one_published_round_per_rfq` — at most one `Published` round per RFQ.
- **One active bid per supplier per round:** `ux_active_supplier_rfq_round` — partial unique index where `status <> 'Withdrawn'`; withdrawn rows remain for history; new Submit allowed after withdrawal.
- **One award per RFQ:** `awards.rfq_id` UNIQUE.
- **Withdrawal consistency:** `chk_bid_withdrawal` ties `Withdrawn` status to `withdrawn_at`.

## Supplier resubmission

1. Supplier submits a bid (`status = 'Submitted'`).
2. Before the round deadline, they may withdraw (`status = 'Withdrawn'`, `withdrawn_at` set).
3. The partial unique index no longer treats that row as the “active” bid.
4. Supplier may insert a **new** bid row for the same `(rfq_id, round_id, supplier_id)`.
5. Old withdrawn bids stay in `bids` / `bid_items` for audit and history.

## Multiple RFQ rounds

- `round_number` is unique per RFQ (`uq_rfq_round_number`).
- After a tied lowest rank, the Approver rejects the outcome (application layer); a **new** round is created with the next `round_number`.
- Prior rounds remain `Closed` / `Evaluated` / `Cancelled` as applicable; old bids and evaluations are not deleted.

## Bid totals and ranking

**Formula (application and function):**

`total_price = SUM(rfq_items.quantity × bid_items.unit_price)` for all lines on the bid.

**Function:** `get_ranked_bids(p_rfq_id, p_round_id)`

- Includes only `bids.status = 'Submitted'`.
- Joins `bid_items` and `rfq_items` on matching `rfq_id`.
- Applies `RANK() OVER (ORDER BY total_price ASC)` so equal totals share the same rank (e.g. 1, 1, 3).

Evaluation persistence copies results into `bid_evaluations` (`total_price`, `rank_position`, `evaluated_by`).

## Equal lowest prices (ties)

When two or more bids share rank 1 with the same total:

- The Approver **rejects** the tied outcome and supplies a reason (stored in `audit_log` and/or `bids.decision_comment` / RFQ workflow in the API).
- The system must **not** auto-award rank 2.
- Procurement creates and publishes a **subsequent round**; suppliers may submit revised bids.

## Files

| File | Contents |
|------|-----------|
| `schema.sql` | Tables, indexes, checks, `get_ranked_bids()` |
| `verify.sql` | Post-deploy sanity queries |
| `README.md` | This document |

## Running `schema.sql` later

On the machine where PostgreSQL is running:

1. Create a database (e.g. `abb_hackathon`) if it does not exist.
2. Run the script as a superuser or owner, for example:

   ```bash
   psql -h <host> -U <user> -d abb_hackathon -f schema.sql
   ```

3. If objects already exist, drop or migrate manually — this script is intended for a **fresh** database.

Update the API `ConnectionStrings:RfqDb` in `appsettings.json` (or environment variables) to point at that host.

## Running `verify.sql`

```bash
psql -h <host> -U <user> -d abb_hackathon -f verify.sql
```

### Expected results

1. **Tables query:** 10 rows — `audit_log`, `awards`, `bid_evaluations`, `bid_items`, `bids`, `rfq_items`, `rfq_rounds`, `rfqs`, `suppliers`, `users`.
2. **Function query:** One row — `get_ranked_bids`.
3. **bid_items constraints:** Multiple rows (`chk_bid_item_price`, `fk_bid_items_bid`, `fk_bid_items_rfq_item`, `uq_bid_item`, etc.).
4. **Ranking sample call:** Zero or more rows; empty is normal without seed data.

## Schema change policy

Do not change `schema.sql` without discussion. Approved changes must update this README and `verify.sql` if new checks are needed.