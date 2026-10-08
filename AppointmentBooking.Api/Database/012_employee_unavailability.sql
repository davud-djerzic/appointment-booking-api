CREATE TABLE employee_unavailability
(
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    employee_id BIGINT NOT NULL,

    block_type SMALLINT NOT NULL,

    starts_at TIMESTAMPTZ NOT NULL,

    ends_at TIMESTAMPTZ NOT NULL,

    is_all_day BOOLEAN NOT NULL DEFAULT FALSE,

    note VARCHAR(500),

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_employee_unavailability_employee
        FOREIGN KEY (employee_id)
        REFERENCES employees(id),

    CONSTRAINT ck_employee_unavailability_type
        CHECK (block_type IN (1, 2)),

    CONSTRAINT ck_employee_unavailability_range
        CHECK (ends_at > starts_at),

    CONSTRAINT ck_employee_unavailability_all_day
        CHECK (
            (block_type = 1 AND is_all_day = FALSE)
            OR (block_type = 2 AND is_all_day = TRUE)
        )
);

CREATE INDEX ix_employee_unavailability_employee_range
    ON employee_unavailability(employee_id, starts_at, ends_at);
