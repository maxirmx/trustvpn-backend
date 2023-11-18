--
-- docker exec -it db psql -U postgres
--

START TRANSACTION;

DROP TABLE IF EXISTS "profiles";

CREATE TABLE "profiles" (
  "id"              SERIAL PRIMARY KEY,
  "name"            VARCHAR(16) NOT NULL
);

INSERT INTO "profiles" ("name") VALUES ("Блокировка");

DROP TABLE IF EXISTS "users";

CREATE TABLE "users" (
  "id"              SERIAL PRIMARY KEY,
  "first_name"      VARCHAR(16) NOT NULL,
  "last_name"       VARCHAR(16) NOT NULL,
  "patronimic"      VARCHAR(16) NOT NULL,
  "email"           VARCHAR(64) NOT NULL,
  "password"        VARCHAR(64) NOT NULL,
  "api_key"         VARCHAR(64) NOT NULL,
  "api_secret"      VARCHAR(64) NOT NULL,
  "is_admin"        BOOLEAN NOT NULL DEFAULT FALSE,
  "profile_id"      INTEGER NOT NULL DEFAULT 0 REFERENCES "profiles" ("id") ON DELETE RESTRICT,

);

CREATE UNIQUE INDEX "idx_users_email" ON "users" ("email");

INSERT INTO "users" ("first_name", "patronimic", "last_name", "email", "password", "api_key", "api_secret", "is_admin") VALUES
('Максим', 'Станиславович', 'Самсонов', 'maxirmx@sw.consulting', '$2a$11$PUWwhEUzqrusmtrDsH4wguSDVx1kmGcksoU1rOKjAcWkGKdGA55ZK', '', '', TRUE, 0);

DROP TABLE IF EXISTS "versions";

CREATE TABLE "versions" (
  "id"      SERIAL PRIMARY KEY,
  "version" VARCHAR(16) NOT NULL,
  "date"    DATE NOT NULL
);

INSERT INTO "versions" ("version", "date") VALUES ('0.1.0', '2023-12-11');

COMMIT;
