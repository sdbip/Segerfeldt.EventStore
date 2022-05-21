CREATE TABLE IF NOT EXISTS Entities (
    "id" TEXT NOT NULL PRIMARY KEY,
    "type" TEXT NOT NULL,
    "version" INT NOT NULL
);

CREATE TABLE IF NOT EXISTS Events (
    "entity" TEXT NOT NULL REFERENCES Entities(id),
    "name" TEXT NOT NULL,
    "details" TEXT NOT NULL,
    "actor" TEXT NOT NULL,
    -- PostgreSQL stores “Julian Day” offset by 12 hours
    "timestamp" FLOAT8 NOT NULL DEFAULT (extract(julian from current_timestamp at time zone 'UTC')) - 0.5,
    "version" INT NOT NULL,
    "position" BIGINT NOT NULL
);

CREATE TABLE IF NOT EXISTS Properties (
    "name" TEXT NOT NULL,
    "value" TEXT NOT NULL
);
