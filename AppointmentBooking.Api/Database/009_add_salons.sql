-- ============================================================
-- SALON
-- ============================================================

CREATE TABLE salons
(
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    name VARCHAR(150) NOT NULL,

    address VARCHAR(255) NOT NULL,
    city VARCHAR(100) NOT NULL,

    instagram_url VARCHAR(255),
    facebook_url VARCHAR(255),

    -- Optional GPS coordinates.
    -- Both values should be NULL or both should be provided.
    latitude NUMERIC(9,6),
    longitude NUMERIC(9,6),

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_salons_latitude
        CHECK (
            latitude IS NULL
            OR latitude BETWEEN -90 AND 90
        ),

    CONSTRAINT ck_salons_longitude
        CHECK (
            longitude IS NULL
            OR longitude BETWEEN -180 AND 180
        ),

    CONSTRAINT ck_salons_coordinates_pair
        CHECK (
            (latitude IS NULL AND longitude IS NULL)
            OR
            (latitude IS NOT NULL AND longitude IS NOT NULL)
        )
);


-- ============================================================
-- SALON WORKING HOURS
-- ============================================================

CREATE TABLE salon_working_hours
(
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    salon_id BIGINT NOT NULL,

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

    CONSTRAINT fk_salon_working_hours_salon
        FOREIGN KEY (salon_id)
        REFERENCES salons(id)
        ON DELETE CASCADE,

    CONSTRAINT ck_salon_working_hours_day_of_week
        CHECK (day_of_week BETWEEN 1 AND 7),

    CONSTRAINT ck_salon_working_hours_time_range
        CHECK (ends_at > starts_at),

    CONSTRAINT ex_salon_working_hours_overlap
        EXCLUDE USING GIST
        (
            salon_id WITH =,
            day_of_week WITH =,
            time_range WITH &&
        )
);


-- ============================================================
-- INDEX
-- ============================================================

CREATE INDEX ix_salon_working_hours_salon_day
    ON salon_working_hours(salon_id, day_of_week);


