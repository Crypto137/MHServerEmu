-- Initialize a new database file using the current schema version

PRAGMA user_version=4;
PRAGMA journal_mode=WAL;

CREATE TABLE "Account" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Email"	TEXT NOT NULL UNIQUE,
	"PlayerName"	TEXT NOT NULL UNIQUE,
	"PasswordHash"	BLOB NOT NULL,
	"Salt"	BLOB NOT NULL,
	"UserLevel"	INTEGER NOT NULL,
	"Flags"	INTEGER NOT NULL,
	PRIMARY KEY("Id")
);

CREATE TABLE "Player" (
	"DbGuid"	INTEGER NOT NULL UNIQUE,
	"ArchiveData"	BLOB,
	"StartTarget"	INTEGER,
	"AOIVolume"	INTEGER,
	"GazillioniteBalance"	INTEGER,
	FOREIGN KEY("DbGuid") REFERENCES "Account"("Id") ON DELETE CASCADE,
	PRIMARY KEY("DbGuid")
);

CREATE TABLE "Avatar" (
	"DbGuid"	INTEGER NOT NULL UNIQUE,
	"ContainerDbGuid"	INTEGER,
	"InventoryProtoGuid"	INTEGER,
	"Slot"	INTEGER,
	"EntityProtoGuid"	INTEGER,
	"ArchiveData"	BLOB,
	FOREIGN KEY("ContainerDbGuid") REFERENCES "Player"("DbGuid") ON DELETE CASCADE,
	PRIMARY KEY("DbGuid")
);

CREATE TABLE "TeamUp" (
	"DbGuid"	INTEGER NOT NULL UNIQUE,
	"ContainerDbGuid"	INTEGER,
	"InventoryProtoGuid"	INTEGER,
	"Slot"	INTEGER,
	"EntityProtoGuid"	INTEGER,
	"ArchiveData"	BLOB,
	FOREIGN KEY("ContainerDbGuid") REFERENCES "Player"("DbGuid") ON DELETE CASCADE,
	PRIMARY KEY("DbGuid")
);

CREATE TABLE "Item" (
	"DbGuid"	INTEGER NOT NULL UNIQUE,
	"ContainerDbGuid"	INTEGER,
	"InventoryProtoGuid"	INTEGER,
	"Slot"	INTEGER,
	"EntityProtoGuid"	INTEGER,
	"ArchiveData"	BLOB,
	FOREIGN KEY("ContainerDbGuid") REFERENCES "Player"("DbGuid") ON DELETE CASCADE,
	FOREIGN KEY("ContainerDbGuid") REFERENCES "Avatar"("DbGuid") ON DELETE CASCADE,
	FOREIGN KEY("ContainerDbGuid") REFERENCES "TeamUp"("DbGuid") ON DELETE CASCADE,
	PRIMARY KEY("DbGuid")
);

CREATE TABLE "ControlledEntity" (
	"DbGuid"	INTEGER NOT NULL UNIQUE,
	"ContainerDbGuid"	INTEGER,
	"InventoryProtoGuid"	INTEGER,
	"Slot"	INTEGER,
	"EntityProtoGuid"	INTEGER,
	"ArchiveData"	BLOB,
	FOREIGN KEY("ContainerDbGuid") REFERENCES "Avatar"("DbGuid") ON DELETE CASCADE,
	PRIMARY KEY("DbGuid")
);

CREATE INDEX "IX_Avatar_ContainerDbGuid" ON "Avatar" ("ContainerDbGuid");
CREATE INDEX "IX_TeamUp_ContainerDbGuid" ON "TeamUp" ("ContainerDbGuid");
CREATE INDEX "IX_Item_ContainerDbGuid" ON "Item" ("ContainerDbGuid");
CREATE INDEX "IX_ControlledEntity_ContainerDbGuid" ON "ControlledEntity" ("ContainerDbGuid");
