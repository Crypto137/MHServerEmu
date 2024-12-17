-- Initialize a new database file using the current schema version

PRAGMA user_version = 2;

CREATE TABLE "Leaderboards" (
	"LeaderboardId"	INTEGER NOT NULL UNIQUE,
	"PrototypeName"	TEXT,
	"ActiveInstanceId"	INTEGER,
	"IsActive"	INTEGER,
	PRIMARY KEY("LeaderboardId")
)

CREATE TABLE "Instances" (
	"InstanceId"	INTEGER NOT NULL UNIQUE,
	"LeaderboardId"	INTEGER NOT NULL,
	"State"	INTEGER,
	"ActivationDate"	INTEGER,
	"Visible"	INTEGER,
	FOREIGN KEY("LeaderboardId") REFERENCES "Leaderboards"("LeaderboardId") ON DELETE CASCADE,
	PRIMARY KEY("InstanceId" AUTOINCREMENT)
)

CREATE TABLE "Entries" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"InstanceId"	INTEGER NOT NULL,
	"AccountId"	INTEGER NOT NULL,
	"GameId"	INTEGER NOT NULL,
	"AvatarId"	INTEGER,
	"Score"	INTEGER,
	"RuleState"	BLOB,
	FOREIGN KEY("InstanceId") REFERENCES "Instances"("InstanceId") ON DELETE CASCADE,
	PRIMARY KEY("Id" AUTOINCREMENT)
)

CREATE INDEX idx_instances_leaderboardid ON Instances (LeaderboardId);
CREATE INDEX idx_entries_instanceid ON Entries (InstanceId);
