CREATE TABLE bookings
(
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    customer_id BIGINT NOT NULL,

    employee_id BIGINT NOT NULL,

    starts_at TIMESTAMPTZ NOT NULL,

    ends_at TIMESTAMPTZ NOT NULL,

    status VARCHAR(20) NOT NULL DEFAULT 'scheduled',

    notes VARCHAR(500),

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    updated_at TIMESTAMPTZ,

    CONSTRAINT fk_bookings_customer
        FOREIGN KEY (customer_id)
        REFERENCES customers(id),

    CONSTRAINT fk_bookings_employee
        FOREIGN KEY (employee_id)
        REFERENCES employees(id),

    CONSTRAINT chk_bookings_period
        CHECK (ends_at > starts_at),

    CONSTRAINT chk_bookings_status
        CHECK
        (
            status IN
            (
                'scheduled',
                'completed',
                'cancelled'
            )
        ),

    CONSTRAINT ex_bookings_employee_time
        EXCLUDE USING GIST
        (
            employee_id WITH =,
            tstzrange(starts_at, ends_at, '[)') WITH &&
        )
        WHERE
        (
            status = 'scheduled'
        )
);


CREATE TABLE appointments
(
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    booking_id BIGINT NOT NULL,

    service_id BIGINT NOT NULL,

    starts_at TIMESTAMPTZ NOT NULL,

    ends_at TIMESTAMPTZ NOT NULL,

    status VARCHAR(20) NOT NULL DEFAULT 'scheduled',

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    updated_at TIMESTAMPTZ,

    CONSTRAINT fk_appointments_booking
        FOREIGN KEY (booking_id)
        REFERENCES bookings(id),

    CONSTRAINT fk_appointments_service
        FOREIGN KEY (service_id)
        REFERENCES services(id),

    CONSTRAINT chk_appointments_period
        CHECK (ends_at > starts_at),

    CONSTRAINT chk_appointments_status
        CHECK
        (
            status IN
            (
                'scheduled',
                'completed',
                'cancelled'
            )
        )
);


CREATE INDEX ix_bookings_customer_id
    ON bookings(customer_id);

CREATE INDEX ix_bookings_employee_starts_at
    ON bookings(employee_id, starts_at);

CREATE INDEX ix_bookings_status
    ON bookings(status);

CREATE INDEX ix_appointments_booking_id
    ON appointments(booking_id);

CREATE INDEX ix_appointments_service_id
    ON appointments(service_id);

CREATE INDEX ix_appointments_status
    ON appointments(status);