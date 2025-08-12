-- Initialize a new database file using the current schema version

PRAGMA user_version = 1;

CREATE TABLE "Leaderboards" (
	"LeaderboardId"	INTEGER NOT NULL PRIMARY KEY,
	"PrototypeName"	TEXT,
	"ActiveInstanceId"	INTEGER,
	"IsEnabled"	INTEGER,
	"StartTime"	INTEGER,
	"MaxResetCount"	INTEGER
);

CREATE TABLE "Instances" (
	"InstanceId"	INTEGER NOT NULL PRIMARY KEY,
	"LeaderboardId"	INTEGER NOT NULL,
	"State"	INTEGER,
	"ActivationDate"	INTEGER,
	"Visible"	INTEGER,
	FOREIGN KEY("LeaderboardId") REFERENCES "Leaderboards"("LeaderboardId") ON DELETE CASCADE
);

CREATE TABLE "Entries" (
	"InstanceId"	INTEGER NOT NULL,
	"ParticipantId"	INTEGER NOT NULL,
	"Score"	INTEGER,
	"HighScore"	INTEGER,
	"RuleStates"	BLOB,
	PRIMARY KEY ("InstanceId", "ParticipantId"),
	FOREIGN KEY("InstanceId") REFERENCES "Instances"("InstanceId") ON DELETE CASCADE
);

CREATE TABLE "MetaEntries" (
	"LeaderboardId"	INTEGER NOT NULL,
	"InstanceId"	INTEGER NOT NULL,
	"SubLeaderboardId"	INTEGER NOT NULL,
	"SubInstanceId"	INTEGER NOT NULL,
	PRIMARY KEY("LeaderboardId", "InstanceId", "SubLeaderboardId"),
	FOREIGN KEY("LeaderboardId") REFERENCES "Leaderboards"("LeaderboardId") ON DELETE CASCADE
);

CREATE TABLE "Rewards" (
	"LeaderboardId"	INTEGER NOT NULL,
	"InstanceId"	INTEGER NOT NULL,
	"ParticipantId"	INTEGER NOT NULL,
	"Rank"	INTEGER NOT NULL,
	"RewardId"	INTEGER NOT NULL,
	"CreationDate"	INTEGER,
	"RewardedDate"	INTEGER,
	PRIMARY KEY ("LeaderboardId", "InstanceId", "ParticipantId"),
	FOREIGN KEY("InstanceId") REFERENCES "Instances"("InstanceId") ON DELETE CASCADE
);

CREATE INDEX idx_instances_leaderboardid ON Instances (LeaderboardId);
CREATE INDEX idx_entries_instanceid ON Entries (InstanceId);
CREATE INDEX idx_rewards_participantid ON Rewards (ParticipantId);
