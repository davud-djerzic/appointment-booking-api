ALTER TABLE bookings
    ALTER COLUMN customer_id DROP NOT NULL;

ALTER TABLE bookings
    ADD COLUMN customer_first_name VARCHAR(50),
    ADD COLUMN customer_last_name VARCHAR(50);

ALTER TABLE bookings
    ADD CONSTRAINT ck_bookings_customer_identity
    CHECK
    (
        (
            customer_id IS NOT NULL
            AND customer_first_name IS NULL
            AND customer_last_name IS NULL
        )
        OR
        (
            customer_id IS NULL
            AND customer_first_name IS NOT NULL
            AND customer_last_name IS NOT NULL
        )
    );
