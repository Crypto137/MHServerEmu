-- Delete old placeholder tables while keeping existing account records
DROP TABLE Avatar;
DROP TABLE Player;

-- Initialize new tables for storing persistent entities
CREATE TABLE "Player" (
	"DbGuid"	INTEGER NOT NULL UNIQUE,
	"ArchiveData"	BLOB,
	"StartTarget"	INTEGER,
	"StartTargetRegionOverride"	INTEGER,
	"AOIVolume"	INTEGER,
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

-- Create indexes for faster container lookup queries
CREATE INDEX "IX_Avatar_ContainerDbGuid" ON "Avatar" ("ContainerDbGuid");
CREATE INDEX "IX_TeamUp_ContainerDbGuid" ON "TeamUp" ("ContainerDbGuid");
CREATE INDEX "IX_Item_ContainerDbGuid" ON "Item" ("ContainerDbGuid");
CREATE INDEX "IX_ControlledEntity_ContainerDbGuid" ON "ControlledEntity" ("ContainerDbGuid");
