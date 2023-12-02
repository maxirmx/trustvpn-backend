--
-- docker exec -it trustvpn-db psql -U postgres
--

START TRANSACTION;

DROP TABLE IF EXISTS "users";
DROP TABLE IF EXISTS "profiles";
DROP TABLE IF EXISTS "versions";

CREATE TABLE "profiles" (
  "id"              SERIAL PRIMARY KEY,
  "name"            VARCHAR(64) NOT NULL,
  "profile"         VARCHAR(64) NOT NULL
);

INSERT INTO "profiles" ("name", "profile") VALUES ('Блокировка', 'none');
INSERT INTO "profiles" ("name", "profile") VALUES ('Ограниченный траффик', 'limited');
INSERT INTO "profiles" ("name", "profile") VALUES ('Неoграниченный траффик', 'unlimited');

CREATE TABLE "users" (
  "id"              SERIAL PRIMARY KEY,
  "first_name"      VARCHAR(16) NOT NULL,
  "last_name"       VARCHAR(16) NOT NULL,
  "patronimic"      VARCHAR(16) NOT NULL,
  "email"           VARCHAR(64) NOT NULL,
  "password"        VARCHAR(64) NOT NULL,
  "is_admin"        BOOLEAN NOT NULL DEFAULT FALSE,
  "profile_id"      INTEGER NOT NULL DEFAULT 0 REFERENCES "profiles" ("id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "idx_users_email" ON "users" ("email");

-- password: Ivanov$123
INSERT INTO "users" ("first_name", "patronimic", "last_name", "email", "password", "is_admin", "profile_id") VALUES
('Иван', 'Иванович', 'Иванов', 'ivanov@example.com', '$2a$11$GAYD9aArfam2L/wxy26r2.gwNSTgaqBN9jZrXsDkc7stvHMYt12XW', TRUE, 1);

CREATE TABLE "versions" (
  "id"      SERIAL PRIMARY KEY,
  "version" VARCHAR(16) NOT NULL,
  "date"    DATE NOT NULL
);

CREATE UNIQUE INDEX "idx_versions_version" ON "versions" ("version");

INSERT INTO "versions" ("version", "date") VALUES ('0.1.3', '2023-12-02');

COMMIT;
