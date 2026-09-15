CREATE EXTENSION IF NOT EXISTS pgcrypto;

ALTER TABLE user_refresh_tokens
ADD COLUMN family_id UUID NOT NULL DEFAULT gen_random_uuid();

ALTER TABLE user_refresh_tokens
ALTER COLUMN family_id DROP DEFAULT;

CREATE INDEX ix_user_refresh_tokens_family_id
    ON user_refresh_tokens(family_id);