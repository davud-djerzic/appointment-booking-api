ALTER TABLE bookings
ADD COLUMN completed_at TIMESTAMPTZ;

ALTER TABLE bookings
ADD COLUMN completion_source VARCHAR(20);

ALTER TABLE bookings
ADD CONSTRAINT chk_bookings_completion_source
CHECK
(
    completion_source IS NULL
    OR completion_source IN
    (
        'manual',
        'automatic'
    )
);

CREATE INDEX ix_bookings_scheduled_ends_at
    ON bookings(ends_at)
    WHERE status = 'scheduled';