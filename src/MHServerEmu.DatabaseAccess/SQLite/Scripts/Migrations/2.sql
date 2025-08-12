-- Add per-account G balance

-- Add new column for G balance to the Player table
ALTER TABLE Player ADD COLUMN GazillioniteBalance INTEGER;

-- Set the value to -1 for all rows, it will be set to the correct value for new accounts when the player logs in
UPDATE Player SET GazillioniteBalance=-1;
