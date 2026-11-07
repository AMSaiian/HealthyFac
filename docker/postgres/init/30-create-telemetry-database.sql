-- Runs once, only when the postgres data volume is empty.
-- The telemetry service owns its own database. It shares the server with the core service.
SELECT 'CREATE DATABASE healthyfac_telemetry'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'healthyfac_telemetry')\gexec
