CREATE TABLE IF NOT EXISTS Entities (
    "id" TEXT NOT NULL,
    "type" TEXT , --NOT NULL,
    "version" INT NOT NULL,

    PRIMARY KEY ("id")
);

CREATE TABLE IF NOT EXISTS Events (
    "entityId" TEXT NOT NULL REFERENCES Entities("id"),
    "entityType" TEXT NOT NULL,
    "name" TEXT NOT NULL,
    "details" TEXT NOT NULL,
    "actor" TEXT NOT NULL,
    "timestamp" REAL DEFAULT (julianday(CURRENT_TIMESTAMP)),
    "version" INT NOT NULL,
    "position" BIGINT NOT NULL
);
