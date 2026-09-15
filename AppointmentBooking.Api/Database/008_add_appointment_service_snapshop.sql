BEGIN;

ALTER TABLE appointments
    ADD COLUMN IF NOT EXISTS service_name_at_booking VARCHAR(150),
    ADD COLUMN IF NOT EXISTS duration_minutes_at_booking INTEGER,
    ADD COLUMN IF NOT EXISTS price_at_booking NUMERIC(10, 2);

UPDATE appointments a
SET
    service_name_at_booking = s.name,
    duration_minutes_at_booking = s.duration_minutes,
    price_at_booking = s.price
FROM services s
WHERE a.service_id = s.id;

ALTER TABLE appointments
    ALTER COLUMN service_name_at_booking SET NOT NULL,
    ALTER COLUMN duration_minutes_at_booking SET NOT NULL,
    ALTER COLUMN price_at_booking SET NOT NULL;

ALTER TABLE appointments
    ADD CONSTRAINT chk_appointments_duration_at_booking
    CHECK (duration_minutes_at_booking > 0);

ALTER TABLE appointments
    ADD CONSTRAINT chk_appointments_price_at_booking
    CHECK (price_at_booking >= 0);

COMMIT;