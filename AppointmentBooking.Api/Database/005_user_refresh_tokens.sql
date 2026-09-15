CREATE TABLE user_refresh_tokens
(
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    user_account_id BIGINT NOT NULL,

    token_hash TEXT NOT NULL,

    expires_at TIMESTAMPTZ NOT NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    revoked_at TIMESTAMPTZ NULL,

    replaced_by_token_id BIGINT NULL,

    CONSTRAINT uq_user_refresh_tokens_token_hash
        UNIQUE (token_hash),

    CONSTRAINT fk_user_refresh_tokens_user_account
        FOREIGN KEY (user_account_id)
        REFERENCES user_accounts(id),

    CONSTRAINT fk_user_refresh_tokens_replaced_by
        FOREIGN KEY (replaced_by_token_id)
        REFERENCES user_refresh_tokens(id)
);


CREATE INDEX ix_user_refresh_tokens_user_account_id
    ON user_refresh_tokens(user_account_id);