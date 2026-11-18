-- ============================================================
-- ABB SUPPLIER RFQ AND BIDDING PORTAL
-- PostgreSQL Database Schema
-- ============================================================

-- 1. USERS
CREATE TABLE users (
    user_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    full_name VARCHAR(150) NOT NULL,
    email VARCHAR(150) NOT NULL,
    password_hash TEXT NOT NULL,
    role VARCHAR(30) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_users_role
        CHECK (role IN (
            'Supplier',
            'ProcurementOfficer',
            'Approver'
        ))
);

CREATE UNIQUE INDEX ux_users_email
ON users (LOWER(email));


-- 2. SUPPLIERS
CREATE TABLE suppliers (
    supplier_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id BIGINT NOT NULL UNIQUE,
    company_name VARCHAR(200) NOT NULL,
    registration_number VARCHAR(100) NOT NULL UNIQUE,
    contact_person VARCHAR(150),
    phone VARCHAR(20),
    approval_status VARCHAR(30) NOT NULL DEFAULT 'Pending',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_suppliers_user
        FOREIGN KEY (user_id)
        REFERENCES users(user_id),

    CONSTRAINT chk_supplier_status
        CHECK (approval_status IN (
            'Pending',
            'Approved',
            'Suspended'
        ))
);


-- 3. RFQS
CREATE TABLE rfqs (
    rfq_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    rfq_number VARCHAR(50) NOT NULL UNIQUE,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    created_by BIGINT NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Draft',
    currency_code CHAR(3) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_rfqs_created_by
        FOREIGN KEY (created_by)
        REFERENCES users(user_id),

    CONSTRAINT chk_rfq_status
        CHECK (status IN (
            'Draft',
            'Published',
            'Closed',
            'Evaluated',
            'Awarded'
        )),

    CONSTRAINT chk_rfq_currency
        CHECK (currency_code ~ '^[A-Z]{3}$')
);


-- 4. RFQ ITEMS
CREATE TABLE rfq_items (
    rfq_item_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    rfq_id BIGINT NOT NULL,
    item_name VARCHAR(200) NOT NULL,
    description TEXT,
    quantity NUMERIC(18,3) NOT NULL,
    unit VARCHAR(30) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_rfq_items_rfq
        FOREIGN KEY (rfq_id)
        REFERENCES rfqs(rfq_id)
        ON DELETE RESTRICT,

    CONSTRAINT chk_rfq_item_quantity
        CHECK (quantity > 0),

    CONSTRAINT uq_rfq_item_id_rfq
        UNIQUE (rfq_item_id, rfq_id)
);


-- 5. RFQ ROUNDS
-- One RFQ can have multiple bidding rounds.
CREATE TABLE rfq_rounds (
    round_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    rfq_id BIGINT NOT NULL,
    round_number INTEGER NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Draft',
    published_at TIMESTAMPTZ,
    deadline TIMESTAMPTZ NOT NULL,
    closed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_rounds_rfq
        FOREIGN KEY (rfq_id)
        REFERENCES rfqs(rfq_id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_rfq_round_number
        UNIQUE (rfq_id, round_number),

    CONSTRAINT uq_round_id_rfq
        UNIQUE (round_id, rfq_id),

    CONSTRAINT chk_round_number
        CHECK (round_number > 0),

    CONSTRAINT chk_round_status
        CHECK (status IN (
            'Draft',
            'Published',
            'Closed',
            'Evaluated',
            'Cancelled'
        )),

    CONSTRAINT chk_round_deadline
        CHECK (
            published_at IS NULL
            OR deadline > published_at
        ),

    CONSTRAINT chk_round_closed_at
        CHECK (
            closed_at IS NULL
            OR (
                published_at IS NOT NULL
                AND closed_at >= published_at
            )
        )
);

-- Only one published round per RFQ at a time.
CREATE UNIQUE INDEX ux_one_published_round_per_rfq
ON rfq_rounds(rfq_id)
WHERE status = 'Published';


-- 6. BIDS
-- Each supplier can submit one active bid per round.
CREATE TABLE bids (
    bid_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    rfq_id BIGINT NOT NULL,
    round_id BIGINT NOT NULL,
    supplier_id BIGINT NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Submitted',
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    withdrawn_at TIMESTAMPTZ,
    decision_comment TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_bids_round
        FOREIGN KEY (round_id, rfq_id)
        REFERENCES rfq_rounds(round_id, rfq_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_bids_supplier
        FOREIGN KEY (supplier_id)
        REFERENCES suppliers(supplier_id)
        ON DELETE RESTRICT,

    CONSTRAINT chk_bid_status
        CHECK (status IN (
            'Submitted',
            'Withdrawn',
            'Accepted',
            'Rejected'
        )),

    CONSTRAINT chk_bid_withdrawal
        CHECK (
            (status = 'Withdrawn' AND withdrawn_at IS NOT NULL)
            OR
            (status <> 'Withdrawn' AND withdrawn_at IS NULL)
        ),

    CONSTRAINT uq_bid_id_rfq_round
        UNIQUE (bid_id, rfq_id, round_id),

    -- Required for the composite FK in bid_items.
    CONSTRAINT uq_bid_id_rfq
        UNIQUE (bid_id, rfq_id)
);

-- Allows resubmission after withdrawal, but not
-- multiple active submissions in the same round.
CREATE UNIQUE INDEX ux_active_supplier_rfq_round
ON bids(rfq_id, round_id, supplier_id)
WHERE status <> 'Withdrawn';


-- 7. BID ITEMS
CREATE TABLE bid_items (
    bid_item_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    bid_id BIGINT NOT NULL,
    rfq_id BIGINT NOT NULL,
    rfq_item_id BIGINT NOT NULL,
    unit_price NUMERIC(18,2) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_bid_items_bid
        FOREIGN KEY (bid_id, rfq_id)
        REFERENCES bids(bid_id, rfq_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_bid_items_rfq_item
        FOREIGN KEY (rfq_item_id, rfq_id)
        REFERENCES rfq_items(rfq_item_id, rfq_id)
        ON DELETE RESTRICT,

    CONSTRAINT chk_bid_item_price
        CHECK (unit_price > 0),

    CONSTRAINT uq_bid_item
        UNIQUE (bid_id, rfq_item_id)
);


-- 8. BID EVALUATIONS
-- Stores the result of evaluating each submitted bid.
-- Equal total prices receive equal ranks.
CREATE TABLE bid_evaluations (
    evaluation_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    bid_id BIGINT NOT NULL,
    rfq_id BIGINT NOT NULL,
    round_id BIGINT NOT NULL,
    total_price NUMERIC(20,2) NOT NULL,
    rank_position INTEGER NOT NULL,
    evaluated_by BIGINT NOT NULL,
    evaluated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_evaluation_bid
        FOREIGN KEY (bid_id, rfq_id, round_id)
        REFERENCES bids(bid_id, rfq_id, round_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_evaluation_user
        FOREIGN KEY (evaluated_by)
        REFERENCES users(user_id),

    CONSTRAINT uq_evaluation_bid
        UNIQUE (bid_id),

    CONSTRAINT chk_evaluation_total
        CHECK (total_price >= 0),

    CONSTRAINT chk_evaluation_rank
        CHECK (rank_position > 0)
);


-- 9. AWARDS
-- One final award per RFQ.
CREATE TABLE awards (
    award_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    rfq_id BIGINT NOT NULL UNIQUE,
    round_id BIGINT NOT NULL,
    bid_id BIGINT NOT NULL UNIQUE,
    awarded_by BIGINT NOT NULL,
    award_comment TEXT NOT NULL,
    awarded_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_awards_rfq
        FOREIGN KEY (rfq_id)
        REFERENCES rfqs(rfq_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_awards_bid
        FOREIGN KEY (bid_id, rfq_id, round_id)
        REFERENCES bids(bid_id, rfq_id, round_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_awards_user
        FOREIGN KEY (awarded_by)
        REFERENCES users(user_id),

    CONSTRAINT chk_award_comment
        CHECK (LENGTH(TRIM(award_comment)) > 0)
);


-- 10. AUDIT LOG
CREATE TABLE audit_log (
    audit_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id BIGINT,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    entity_id BIGINT NOT NULL,
    old_values JSONB,
    new_values JSONB,
    ip_address INET,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_audit_user
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON DELETE SET NULL
);


-- ============================================================
-- INDEXES
-- ============================================================

CREATE INDEX ix_rfqs_status
ON rfqs(status);

CREATE INDEX ix_rfqs_created_by
ON rfqs(created_by);

CREATE INDEX ix_rfq_items_rfq
ON rfq_items(rfq_id);

CREATE INDEX ix_suppliers_approval
ON suppliers(approval_status);

CREATE INDEX ix_rounds_rfq_status_deadline
ON rfq_rounds(rfq_id, status, deadline);

CREATE INDEX ix_bids_rfq_round_status
ON bids(rfq_id, round_id, status);

CREATE INDEX ix_bids_supplier
ON bids(supplier_id, rfq_id);

CREATE INDEX ix_bid_items_rfq_item
ON bid_items(rfq_item_id);

CREATE INDEX ix_evaluations_round_rank
ON bid_evaluations(round_id, rank_position);

CREATE INDEX ix_audit_entity
ON audit_log(entity_type, entity_id, created_at DESC);

CREATE INDEX ix_audit_user_created
ON audit_log(user_id, created_at DESC);


-- ============================================================
-- BID RANKING FUNCTION
-- Calculates total bid value using RFQ item quantities.
-- RANK() gives equal prices the same rank.
-- ============================================================

CREATE OR REPLACE FUNCTION get_ranked_bids(
    p_rfq_id BIGINT,
    p_round_id BIGINT
)
RETURNS TABLE (
    bid_id BIGINT,
    supplier_id BIGINT,
    total_price NUMERIC,
    rank_position BIGINT
)
LANGUAGE SQL
STABLE
AS $$
    WITH bid_totals AS (
        SELECT
            b.bid_id,
            b.supplier_id,
            SUM(ri.quantity * bi.unit_price) AS total_price
        FROM bids b
        JOIN bid_items bi
            ON bi.bid_id = b.bid_id
        JOIN rfq_items ri
            ON ri.rfq_item_id = bi.rfq_item_id
           AND ri.rfq_id = b.rfq_id
        WHERE b.rfq_id = p_rfq_id
          AND b.round_id = p_round_id
          AND b.status = 'Submitted'
        GROUP BY b.bid_id, b.supplier_id
    )
    SELECT
        bt.bid_id,
        bt.supplier_id,
        bt.total_price,
        RANK() OVER (ORDER BY bt.total_price ASC)
    FROM bid_totals bt
    ORDER BY bt.total_price ASC;
$$;

-- ============================================================
-- END OF SCHEMA
-- ============================================================