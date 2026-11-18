# ABB Supplier RFQ and Bidding Portal

Full-stack procurement portal: ASP.NET Core Web API (Dapper + PostgreSQL + JWT) and Angular frontend.

## Repository layout

| Path | Description |
|------|-------------|
| `RFQAPI/` | ASP.NET Core Web API (`RFQAPI.slnx`, project `RFQAPI/RFQAPI/`) |
| `database/` | PostgreSQL scripts — see [database/README.md](database/README.md) |

## Technology

- **Backend:** C# / ASP.NET Core 9 Web API, Dapper, Npgsql, JWT, Swagger (Swashbuckle)
- **Password hashing:** BCrypt (`BCrypt.Net-Next`)
- **Frontend:** Angular (CORS: `http://localhost:4200`)
- **Database:** PostgreSQL (scripts in `database/`; server may be on another machine)

**Architecture:** Controller → Service → Repository → PostgreSQL (no Entity Framework, no Docker).

## NuGet packages (API)

| Package | Purpose |
|---------|---------|
| `Dapper` | Data access |
| `Npgsql` | PostgreSQL driver |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT authentication |
| `BCrypt.Net-Next` | Password hashing |
| `Swashbuckle.AspNetCore` | Swagger UI |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (project targets `net9.0`)
- PostgreSQL with schema applied (when testing against a real database)
- Node.js / Angular CLI (frontend, later)

## Configuration

### Connection string

`RFQAPI/RFQAPI/appsettings.json`:

```json
"ConnectionStrings": {
  "RfqDb": "Host=<host>;Port=5432;Database=abb_hackathon;Username=<user>;Password=<secret>;"
}
```

Override the password via environment variables or User Secrets — do not commit production credentials.

### JWT

`appsettings.json` defines issuer, audience, and expiration. The **signing key must be at least 32 characters** and must not be committed for production.

**Option A – User Secrets (recommended):**

```bash
cd RFQAPI/RFQAPI
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "YourSecretKey_MustBeAtLeast32Characters!"
```

**Option B – Development only:** `appsettings.Development.json` (gitignored) may set `Jwt:Key` for local runs.

```json
"Jwt": {
  "Issuer": "RFQAPI",
  "Audience": "RFQAPI",
  "Key": "<from secrets>",
  "ExpirationMinutes": 60
}
```

## Run the API

```bash
cd RFQAPI/RFQAPI
dotnet restore
dotnet run
```

- HTTP: `http://localhost:5013`
- HTTPS: `https://localhost:7061`
- Swagger: `http://localhost:5013/swagger` (Development)

## Module 1 – Authentication and user management

### Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/auth/register` | None | Supplier self-registration: creates `users` + `suppliers` (`Pending`) in one transaction |
| `POST` | `/api/auth/login` | None | Login; returns JWT + user profile |
| `GET` | `/api/users/me` | Bearer | Current user profile |
| `GET` | `/api/users?page=1&pageSize=20` | Bearer, **Approver** | Paginated user list |
| `PATCH` | `/api/users/{id}/active` | Bearer, **Approver** | Activate/deactivate user |

ProcurementOfficer and Approver accounts are **not** created via API; seed or insert manually in PostgreSQL.

### Sample requests

**Register**

```http
POST /api/auth/register
Content-Type: application/json

{
  "fullName": "Jane Doe",
  "email": "supplier@example.com",
  "password": "Password123!",
  "companyName": "Acme Supplies Ltd",
  "registrationNumber": "REG-10001",
  "contactPerson": "Jane Doe",
  "phone": "+15551234567"
}
```

**Response (201):** `user` and `supplier` objects — no password hash. Supplier `approvalStatus` is `Pending`.

**Login**

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "supplier@example.com",
  "password": "Password123!"
}
```

**Response (200):**

```json
{
  "token": "<jwt>",
  "expiresAt": "2026-11-18T18:00:00Z",
  "user": { "userId": 1, "fullName": "...", "email": "...", "role": "Supplier", "isActive": true, "createdAt": "..." }
}
```

**Get me**

```http
GET /api/users/me
Authorization: Bearer <token>
```

**List users (Approver)**

```http
GET /api/users?page=1&pageSize=20
Authorization: Bearer <approver-token>
```

**Deactivate user (Approver)**

```http
PATCH /api/users/2/active
Authorization: Bearer <approver-token>
Content-Type: application/json

{ "isActive": false }
```

Cannot deactivate your own account (`400`).

### HTTP status codes (Module 1)

| Code | When |
|------|------|
| 200 | Success |
| 201 | Registered |
| 400 | Validation / bad request / self-deactivate |
| 401 | Invalid login, inactive account, or missing/invalid JWT |
| 403 | Wrong role (e.g. Supplier calling `/api/users`) |
| 404 | User not found |
| 409 | Duplicate email or registration number |
| 500 | Unexpected server error (no internal details in body) |

## Module 2 – Supplier management

### Business rules

- Registration creates a supplier with `approval_status = Pending`.
- Only **Approved** suppliers may bid (enforced in Module 5).
- Suppliers may update **company name**, **contact person**, and **phone** only — not registration number or approval status.
- Procurement Officers review suppliers: **Approved** or **Suspended** only (not back to **Pending**).

### Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/api/suppliers/me` | Bearer, **Supplier** | Own supplier profile |
| `PUT` | `/api/suppliers/me` | Bearer, **Supplier** | Update own profile |
| `GET` | `/api/suppliers` | Bearer, **ProcurementOfficer** or **Approver** | Paginated list; optional `approvalStatus`, `companyName` (partial match) |
| `GET` | `/api/suppliers/{supplierId}` | Bearer, **ProcurementOfficer** or **Approver** | Supplier + linked user details (no password) |
| `PATCH` | `/api/suppliers/{supplierId}/status` | Bearer, **ProcurementOfficer** | Set `Approved` or `Suspended`; writes `audit_log` in same DB transaction |

Default page size: **20**; maximum: **100**.

**Review supplier (ProcurementOfficer)**

```http
PATCH /api/suppliers/1/status
Authorization: Bearer <po-token>
Content-Type: application/json

{ "approvalStatus": "Approved" }
```

Audit row: `action = SupplierStatusUpdated`, `entity_type = Supplier`, `old_values` / `new_values` JSON with `approval_status`.

### HTTP status codes (Module 2)

| Code | When |
|------|------|
| 404 | Supplier profile not found |
| 400 | Invalid status (e.g. `Pending`), invalid pagination, invalid ID |
| 409 | Duplicate registration number on register |

## Module 3 – RFQ management (draft workflow)

### Business rules

- **ProcurementOfficer** creates and edits RFQs; **Approver** can view only (no edit in this module).
- Suppliers cannot call these endpoints.
- New RFQs start as **Draft**; edit allowed only in **Draft** and only by the **creating** officer.
- RFQ number is immutable after create; at least **one item** required; currency is 3-letter ISO-style (`currency_code`).
- All line items are treated as mandatory (no `is_mandatory` column). Rounds, bids, and publish are **not** in this module.

### Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/rfqs` | **ProcurementOfficer** | Create draft RFQ + items (transaction + audit) |
| `PUT` | `/api/rfqs/{rfqId}` | **ProcurementOfficer** | Update draft RFQ; replace items (transaction + audit) |
| `GET` | `/api/rfqs/{rfqId}` | **ProcurementOfficer**, **Approver** | RFQ detail with all items |
| `GET` | `/api/rfqs` | **ProcurementOfficer**, **Approver** | Paginated summary; optional `status`, `rfqNumber`, `title` filters |

Default page size **20**, max **100**. List responses omit line items.

**Create sample**

```http
POST /api/rfqs
Authorization: Bearer <po-token>
Content-Type: application/json

{
  "rfqNumber": "RFQ-2026-001",
  "title": "Industrial Motors Procurement",
  "currencyCode": "USD",
  "items": [
    { "itemName": "3-Phase Motor 5HP", "quantity": 25, "unit": "EA" },
    { "itemName": "VFD 5HP", "quantity": 25, "unit": "EA" }
  ]
}
```

Audit actions: `RfqCreated`, `RfqUpdated` on entity type `Rfq` (same transaction as RFQ writes).

### HTTP status codes (Module 3)

| Code | When |
|------|------|
| 201 | RFQ created |
| 400 | Validation, non-draft update, wrong creator |
| 404 | RFQ not found |
| 409 | Duplicate `rfq_number` |

## Module 4 – RFQ round management

### Round lifecycle

| Step | Round status | RFQ status |
|------|--------------|------------|
| Create round 1 | `Draft` | `Draft` (unchanged) |
| Publish | `Published` | `Published` |
| Close (manual) | `Closed` | `Closed` |
| Evaluate (Module 6) | `Evaluated` | `Evaluated` |
| Create next round | new `Draft` | must be `Evaluated` + prior round `Evaluated` |
| Publish next round | `Published` | `Published` again |

No background jobs; Module 5 enforces deadlines even if the round is not manually closed.

### Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/rfqs/{rfqId}/rounds` | **ProcurementOfficer** | Create round (`deadline` only; auto `round_number`) |
| `PATCH` | `/api/rfqs/{rfqId}/rounds/{roundId}/publish` | **ProcurementOfficer** | Publish draft round |
| `PATCH` | `/api/rfqs/{rfqId}/rounds/{roundId}/close` | **ProcurementOfficer** | Close published round |
| `GET` | `/api/rfqs/{rfqId}/rounds` | **ProcurementOfficer**, **Approver** | All rounds by `round_number` |
| `GET` | `/api/rfqs/{rfqId}/rounds/{roundId}` | **ProcurementOfficer**, **Approver** | Single round |

Audit: `RfqRoundCreated`, `RfqRoundPublished`, `RfqRoundClosed` (entity `RfqRound`) in the same transaction as status changes.

### HTTP status codes (Module 4)

| Code | When |
|------|------|
| 201 | Round created |
| 400 | Past deadline, wrong RFQ/round state, publish/close invalid |
| 404 | RFQ or round not found / mismatch |
| 409 | Concurrent duplicate `round_number` or second published round |

## Module 5 – Bid management (suppliers)

### Rules

- **Approved** suppliers only; RFQ and round must be **Published**.
- Bidding allowed only while **`deadline > CURRENT_TIMESTAMP`** in PostgreSQL (even if the round was not manually closed).
- Quote **every** RFQ line; unit price **> 0**; currency is the RFQ’s `currency_code` (not sent in the request).
- One **active** bid per supplier per round (`ux_active_supplier_rfq_round`); withdrawn bids are kept; resubmit creates a **new** bid row.
- No rankings or totals stored here (Module 6).

### Endpoints (Supplier only)

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/rfqs/{rfqId}/rounds/{roundId}/bids` | Submit bid + line prices |
| `PATCH` | `/api/bids/{bidId}/withdraw` | Withdraw before deadline |
| `GET` | `/api/bids/me` | Paginated own bids; optional `rfqId`, `status` |
| `GET` | `/api/bids/{bidId}` | Own bid detail + line items |

Audit: `BidSubmitted`, `BidWithdrawn` (entity `Bid`) in the same transaction as bid writes.

### HTTP status codes (Module 5)

| Code | When |
|------|------|
| 201 | Bid created |
| 400 | Validation, deadline, wrong RFQ/round/bid status |
| 403 | Supplier not approved |
| 404 | No profile / RFQ-round / bid (no cross-supplier leakage) |
| 409 | Active bid already exists |

## Module 6 – Bid evaluation and ranking

### Rules

- **ProcurementOfficer** runs evaluation; **PO** and **Approver** view results (suppliers **403**).
- RFQ and round must be **Closed** (manual close in Module 4).
- Only **`Submitted`** bids; withdrawn bids excluded.
- Totals: `SUM(rfq_items.quantity × bid_items.unit_price)` via existing `get_ranked_bids()`.
- Ranking: PostgreSQL `RANK() OVER (ORDER BY total_price ASC)` — equal totals share rank (e.g. 1, 1, 3).
- **`isLowestRankTied`** when multiple bids share rank 1 — no auto-award; Module 7 handles ties.
- Persists `bid_evaluations`; sets RFQ and round to **Evaluated**.

### Endpoints

| Method | Route | Auth |
|--------|-------|------|
| `POST` | `/api/rfqs/{rfqId}/rounds/{roundId}/evaluate` | ProcurementOfficer |
| `GET` | `/api/rfqs/{rfqId}/rounds/{roundId}/evaluation` | PO, Approver |
| `GET` | `/api/rfqs/{rfqId}/rounds/{roundId}/evaluation/{bidId}` | PO, Approver |

Audit: `RfqRoundEvaluated` on entity `RfqRound` (same transaction).

### HTTP status codes (Module 6)

| Code | When |
|------|------|
| 200 | Evaluation completed / results returned |
| 400 | Not closed, no bids, incomplete bid lines |
| 404 | RFQ/round/bid or evaluation missing |
| 409 | Round already evaluated |

## Module 7 – Approver review and tie resolution

Decisions are stored in **`audit_log`** only (no new tables): `EvaluationApproved` / `EvaluationRejected` on entity `RfqRound`.

### Workflow

| Scenario | Approver action | RFQ / round status | Next step |
|----------|-----------------|--------------------|-----------|
| Unique lowest rank | `Approved` | Stay **Evaluated** | Module 8 award |
| Tied lowest rank | `Rejected` + reason | Stay **Evaluated** | PO creates next round (Module 4) |

- **Approve** only when exactly one bid has the lowest rank.
- **Reject** only when multiple bids share the lowest rank (reason required).
- No awards, no bid `Accepted`/`Rejected` in this module.

### Endpoints

| Method | Route | Auth |
|--------|-------|------|
| `POST` | `/api/rfqs/{rfqId}/rounds/{roundId}/evaluation/decision` | **Approver** |
| `GET` | `/api/rfqs/{rfqId}/rounds/{roundId}/evaluation/decision` | PO, Approver |

GET returns `status: "Pending"` when evaluation exists but no decision yet. Duplicate decision → **409** (transaction + round lock).

### Module 4 integration

After **Rejected**, RFQ and latest round remain **Evaluated** — matches Module 4 rules for creating the next round. **No Module 4 code changes.**

## Module 8 – Award management

### Rules

- **Approver** creates award; explicit `bidId` in request (no auto-selection).
- Requires **`EvaluationApproved`** audit for that round; **Rejected** or **Pending** → **400**.
- RFQ + round must be **Evaluated**; bid must be **Submitted**, rank **1**, **unique** lowest rank (recomputed from `bid_evaluations`).
- One award per RFQ (`awards.rfq_id` unique); RFQ → **Awarded**; round stays **Evaluated**.
- Bid statuses are not changed to Accepted/Rejected.

### Endpoints

| Method | Route | Auth |
|--------|-------|------|
| `POST` | `/api/rfqs/{rfqId}/rounds/{roundId}/award` | Approver |
| `GET` | `/api/rfqs/{rfqId}/award` | PO, Approver |
| `GET` | `/api/awards` | PO, Approver (paginated; `rfqNumber`, `companyName` filters) |

Audit: `RfqAwarded` on entity **Rfq** (same transaction as insert).

## Module 9 – Audit log retrieval (read-only)

**ProcurementOfficer** and **Approver** only. Suppliers receive **403**.

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/audit-logs` | Paginated list with optional filters |
| `GET` | `/api/audit-logs/{auditId}` | Single record |
| `GET` | `/api/audit-logs/entity/{entityType}/{entityId}` | Entity history (paginated) |

**Query parameters (list):** `page` (default 1), `pageSize` (default 10, max 100), `action`, `entityType`, `entityId`, `userId`, `fromDate`, `toDate`.

- Sort: `created_at DESC`.
- `fromDate` / `toDate` are interpreted as **UTC** `DateTimeOffset` values.
- Response includes `totalCount` and `totalPages`.
- Actor: `users` join (`fullName`, `email` only); `user_id` null → `actor` null.
- `old_values` / `new_values` returned as JSON objects (`JsonElement`).

Uses existing indexes `ix_audit_entity` and `ix_audit_user_created`. No write/update/delete endpoints.

### Testing when PostgreSQL is available

1. Run [database/schema.sql](database/schema.sql) on the target database.
2. Point `RfqDb` at that host.
3. Set `Jwt:Key` (User Secrets).
4. `dotnet run` and open Swagger.
5. **Register** a supplier → **Login** → **GET /api/suppliers/me** and **GET /api/users/me**.
6. **ProcurementOfficer testing:** set role in SQL, then **GET /api/suppliers** and **PATCH .../status**.
7. **Approver testing:** insert or update a user in SQL, for example:

   ```sql
   UPDATE users SET role = 'Approver' WHERE email = 'supplier@example.com';
   ```

   Log in again to get a token with the `Approver` role, then test `GET /api/users` and `PATCH .../active`.

**Note:** Database operations are implemented against the schema in `database/schema.sql` but have **not** been integration-tested in this environment (PostgreSQL on another laptop).

## Database setup

See [database/README.md](database/README.md), [database/schema.sql](database/schema.sql), and [database/verify.sql](database/verify.sql).

## Implementation status

| Module | Status |
|--------|--------|
| Database scripts | Ready |
| Module 1 – Auth & users | **Implemented** (compile verified; DB test pending) |
| Module 2 – Suppliers | **Implemented** (compile verified; DB test pending) |
| Module 3 – RFQs | **Implemented** (compile verified; DB test pending) |
| Module 4 – Rounds | **Implemented** (compile verified; DB test pending) |
| Module 5 – Bids | **Implemented** (compile verified; DB test pending) |
| Module 6 – Evaluation | **Implemented** (compile verified; DB test pending) |
| Module 7 – Approval / ties | **Implemented** (compile verified; DB test pending) |
| Module 8 – Awards | **Implemented** (compile verified; DB test pending) |
| Module 9 – Audit retrieval | **Implemented** (compile verified; DB test pending) |