-- Add guild data.

-- Add last logout time to players and initialize it to 0.
ALTER TABLE Player ADD COLUMN LastLogoutTime INTEGER;
UPDATE Player SET LastLogoutTime=0;

-- Create guild tables.
CREATE TABLE "Guild" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Name"	TEXT NOT NULL UNIQUE,
	"Motd"	TEXT NOT NULL,
	"CreatorDbGuid"	INTEGER,
	"CreationTime"	INTEGER,
	PRIMARY KEY("Id")
);

CREATE TABLE "GuildMember" (
	"PlayerDbGuid"	INTEGER NOT NULL UNIQUE,
	"GuildId"	INTEGER NOT NULL,
	"Membership"	INTEGER NOT NULL,
	FOREIGN KEY("PlayerDbGuid") REFERENCES "Player"("DbGuid") ON DELETE CASCADE,
	FOREIGN KEY("GuildId") REFERENCES "Guild"("Id") ON DELETE CASCADE,
	PRIMARY KEY("PlayerDbGuid")
);

-- We do not need indexing for these tables because we are going to load all guilds into memory.
