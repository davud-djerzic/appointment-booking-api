CREATE TABLE employee_working_hours
(
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    employee_id BIGINT NOT NULL,

    day_of_week SMALLINT NOT NULL,

    starts_at TIME NOT NULL,

    ends_at TIME NOT NULL,

    time_range INT4RANGE GENERATED ALWAYS AS
    (
        int4range(
            EXTRACT(EPOCH FROM starts_at)::integer,
            EXTRACT(EPOCH FROM ends_at)::integer,
            '[)'
        )
    ) STORED,

    CONSTRAINT fk_employee_working_hours_employee
        FOREIGN KEY (employee_id)
        REFERENCES employees(id),

    CONSTRAINT ck_employee_working_hours_day_of_week
        CHECK (day_of_week BETWEEN 1 AND 7),

    CONSTRAINT ck_employee_working_hours_time_range
        CHECK (ends_at > starts_at),

    CONSTRAINT ex_employee_working_hours_overlap
        EXCLUDE USING GIST
        (
            employee_id WITH =,
            day_of_week WITH =,
            time_range WITH &&
        )
);

CREATE INDEX ix_employee_working_hours_employee_day
    ON employee_working_hours(employee_id, day_of_week);