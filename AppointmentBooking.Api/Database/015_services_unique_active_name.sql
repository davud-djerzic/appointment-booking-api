-- Service names must be unique only among active services.
-- A deleted (inactive) service keeps its row, so its name can be used again
-- for a new service. Names are compared case-insensitively ("Šišanje" = "šišanje").
--
-- Before running on an existing database, check there are no active duplicates
-- (this query must return no rows, otherwise creating the index fails):
--
--   SELECT lower(name), count(*)
--   FROM services
--   WHERE is_active = TRUE
--   GROUP BY lower(name)
--   HAVING count(*) > 1;

BEGIN;

ALTER TABLE services
    DROP CONSTRAINT IF EXISTS services_name_key;

CREATE UNIQUE INDEX IF NOT EXISTS ux_services_active_name
    ON services (lower(name))
    WHERE is_active = TRUE;

COMMIT;
