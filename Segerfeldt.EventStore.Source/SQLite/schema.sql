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
    timestamp DECIMAL(12,7) DEFAULT (strftime('%s', CURRENT_TIMESTAMP) / 86400.0),
    version INT NOT NULL,
    position BIGINT NOT NULL
);
