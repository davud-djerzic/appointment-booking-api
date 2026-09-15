CREATE EXTENSION IF NOT EXISTS btree_gist;
CREATE EXTENSION IF NOT EXISTS citext;

CREATE TABLE user_accounts
(
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,

    email CITEXT NOT NULL,

    password_hash TEXT NOT NULL,

    role VARCHAR(20) NOT NULL,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_user_accounts_email
        UNIQUE (email),

    CONSTRAINT ck_user_accounts_role
        CHECK (role IN ('customer', 'employee', 'admin'))
);

CREATE TABLE customers
(
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    user_account_id BIGINT NOT NULL,

    phone VARCHAR(30) NOT NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_customers_user_account
        UNIQUE (user_account_id),

    CONSTRAINT fk_customers_user_account
        FOREIGN KEY (user_account_id)
        REFERENCES user_accounts(id)
);


CREATE TABLE employees
(
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    user_account_id BIGINT NOT NULL,

    phone VARCHAR(30) NOT NULL,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_employees_user_account
        UNIQUE (user_account_id),

    CONSTRAINT fk_employees_user_account
        FOREIGN KEY (user_account_id)
        REFERENCES user_accounts(id)
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
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

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


CREATE INDEX ix_employee_services_service_id
    ON employee_services(service_id);