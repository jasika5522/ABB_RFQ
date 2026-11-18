-- ============================================================
-- ABB RFQ Portal – Database verification script
-- Run after executing schema.sql on PostgreSQL.
-- ============================================================

-- Check all tables
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_type = 'BASE TABLE'
ORDER BY table_name;

-- Expected: audit_log, awards, bid_evaluations, bid_items, bids,
--           rfq_items, rfq_rounds, rfqs, suppliers, users (10 rows)

-- Check ranking function
SELECT routine_name
FROM information_schema.routines
WHERE routine_schema = 'public'
  AND routine_name = 'get_ranked_bids';

-- Check bid_items constraints
SELECT
    conname,
    contype
FROM pg_constraint
WHERE conrelid = 'bid_items'::regclass
ORDER BY conname;

-- Execute ranking function (may return no rows without sample data)
SELECT *
FROM get_ranked_bids(1::BIGINT, 1::BIGINT);