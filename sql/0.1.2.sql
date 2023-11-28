--
-- docker exec -it db psql -U postgres
--

START TRANSACTION;

CREATE UNIQUE INDEX "idx_versions_version" ON "versions" ("version");

INSERT INTO "versions" ("version", "date") VALUES
('0.1.2', '2023-11-29');

COMMIT;
