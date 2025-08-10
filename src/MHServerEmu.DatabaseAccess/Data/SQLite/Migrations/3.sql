-- Game instancing update.

-- Remove the deprecated start target override column.
ALTER TABLE Player DROP COLUMN StartTargetRegionOverride;

-- Switch the database to the WAL mode to allow concurrent reads and writes.
PRAGMA journal_mode=WAL;
