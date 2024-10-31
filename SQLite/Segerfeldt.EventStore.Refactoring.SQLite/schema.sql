CREATE TABLE IF NOT EXISTS Entities (
    id TEXT NOT NULL,
    type TEXT NOT NULL,
    version INT NOT NULL,

    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS Events (
    entity_id TEXT NOT NULL REFERENCES Entities(id),
    name TEXT NOT NULL,
    details TEXT NOT NULL,
    actor TEXT NOT NULL,
    timestamp DECIMAL(12,7) DEFAULT (JulianDay(CURRENT_TIMESTAMP) - 2440587.5),
    ordinal INT NOT NULL,
    position BIGINT NOT NULL
);
