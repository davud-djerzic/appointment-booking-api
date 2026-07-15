CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE TABLE employees
(
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    email VARCHAR(254) NOT NULL UNIQUE,
    phone VARCHAR(30) NOT NULL,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE TABLE services
(
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    name VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(500),

    duration_minutes INTEGER NOT NULL,
    price NUMERIC(10, 2) NOT NULL,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,

    CONSTRAINT chk_services_duration
        CHECK (duration_minutes > 0),

    CONSTRAINT chk_services_price
        CHECK (price >= 0)
);

CREATE TABLE employee_services
(
    employee_id BIGINT NOT NULL,
    service_id BIGINT NOT NULL,

    PRIMARY KEY (employee_id, service_id),

    CONSTRAINT fk_employee_services_employee
        FOREIGN KEY (employee_id)
        REFERENCES employees(id),

    CONSTRAINT fk_employee_services_service
        FOREIGN KEY (service_id)
        REFERENCES services(id)
);

CREATE TABLE appointments
(
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    employee_id BIGINT NOT NULL,
    service_id BIGINT NOT NULL,

    customer_first_name VARCHAR(100),
    customer_last_name VARCHAR(100),
    customer_email VARCHAR(254),
    customer_phone VARCHAR(30),

    starts_at TIMESTAMPTZ NOT NULL,
    ends_at TIMESTAMPTZ NOT NULL,

    status VARCHAR(20) NOT NULL DEFAULT 'held',

    hold_token UUID UNIQUE,
    hold_expires_at TIMESTAMPTZ,

    notes VARCHAR(500),

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,

    CONSTRAINT fk_appointments_employee_service
        FOREIGN KEY (employee_id, service_id)
        REFERENCES employee_services(employee_id, service_id),

    CONSTRAINT chk_appointments_period
        CHECK (ends_at > starts_at),

    CONSTRAINT chk_appointments_status
        CHECK
        (
            status IN
            (
                'held',
                'scheduled',
                'completed',
                'cancelled'
            )
        ),

    CONSTRAINT chk_appointments_hold_data
        CHECK
        (
            (
                status = 'held'
                AND hold_token IS NOT NULL
                AND hold_expires_at IS NOT NULL
                AND hold_expires_at > created_at
            )
            OR
            (
                status <> 'held'
                AND hold_token IS NULL
                AND hold_expires_at IS NULL
            )
        ),

    CONSTRAINT chk_appointments_customer_data
        CHECK
        (
            status = 'held'
            OR
            (
                customer_first_name IS NOT NULL
                AND customer_last_name IS NOT NULL
                AND customer_email IS NOT NULL
            )
        ),

    CONSTRAINT ex_appointments_employee_time
        EXCLUDE USING GIST
        (
            employee_id WITH =,
            tstzrange(starts_at, ends_at, '[)') WITH &&
        )
        WHERE
        (
            status IN ('held', 'scheduled')
        )
);

CREATE INDEX ix_employee_services_service_id
    ON employee_services(service_id);

CREATE INDEX ix_appointments_employee_starts_at
    ON appointments(employee_id, starts_at);

CREATE INDEX ix_appointments_service_id
    ON appointments(service_id);

CREATE INDEX ix_appointments_status
    ON appointments(status);

CREATE INDEX ix_appointments_customer_email
    ON appointments(customer_email);

CREATE INDEX ix_appointments_hold_expires_at
    ON appointments(hold_expires_at)
    WHERE status = 'held';