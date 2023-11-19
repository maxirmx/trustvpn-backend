--
-- docker exec -it o-db psql -U postgres
--

START TRANSACTION;

DROP TABLE IF EXISTS "users";
DROP TABLE IF EXISTS "profiles";
DROP TABLE IF EXISTS "versions";

CREATE TABLE "profiles" (
  "id"              SERIAL PRIMARY KEY,
  "name"            VARCHAR(64) NOT NULL
);

INSERT INTO "profiles" ("name") VALUES ('Блокировка');
INSERT INTO "profiles" ("name") VALUES ('Ограниченный траффик');
INSERT INTO "profiles" ("name") VALUES ('Неoграниченный траффик');

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

--
-- password: 12345
-- Change immediately !!!
--\q
INSERT INTO "users" ("first_name", "patronimic", "last_name", "email", "password", "is_admin", "profile_id") VALUES
('Максим', 'Станиславович', 'Самсонов', 'maxirmx@sw.consulting', '$2a$11$k44i2k4/0sCdFnVqsll0QeTusjJjbAVwbT19gsfjLJRCA5ocbBnVu', TRUE, 1);

CREATE TABLE "versions" (
  "id"      SERIAL PRIMARY KEY,
  "version" VARCHAR(16) NOT NULL,
  "date"    DATE NOT NULL
);

INSERT INTO "versions" ("version", "date") VALUES ('0.1.0', '2023-12-11');

COMMIT;
