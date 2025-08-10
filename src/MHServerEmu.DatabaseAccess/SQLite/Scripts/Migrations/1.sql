-- Convert individual flag columns into a single bit field

-- Reuse existing IsBanned column, allowing us to keep ban information
ALTER TABLE Account RENAME COLUMN IsBanned TO Flags;

-- Get rid of archived and password expired columns
ALTER TABLE Account DROP COLUMN IsArchived;
ALTER TABLE Account DROP COLUMN IsPasswordExpired;
