CREATE TABLE password_reset_codes (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    user_account_id BIGINT NOT NULL,

    code_hash TEXT NOT NULL,

    expires_at TIMESTAMPTZ NOT NULL,

    attempts SMALLINT NOT NULL DEFAULT 0,

    verified_at TIMESTAMPTZ NULL,

    used_at TIMESTAMPTZ NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_password_reset_codes_user_account
        FOREIGN KEY (user_account_id)
        REFERENCES user_accounts(id),

    CONSTRAINT ck_password_reset_codes_attempts
        CHECK (attempts >= 0)
);

CREATE INDEX ix_password_reset_codes_user_account_id
    ON password_reset_codes(user_account_id);

CREATE INDEX ix_password_reset_codes_user_account_created_at
    ON password_reset_codes(user_account_id, created_at DESC);