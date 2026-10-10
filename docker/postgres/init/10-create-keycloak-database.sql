-- Runs once, only when the postgres data volume is empty.
-- Keycloak needs its own database. It shares the server with the core service.
SELECT 'CREATE DATABASE keycloak'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'keycloak')\gexec
