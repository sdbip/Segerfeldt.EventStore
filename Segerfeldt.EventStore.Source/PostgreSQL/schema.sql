CREATE TABLE IF NOT EXISTS Entities (
    id TEXT NOT NULL PRIMARY KEY,
    type TEXT NOT NULL,
    version INT NOT NULL
);

CREATE TABLE IF NOT EXISTS Events (
    entity_id TEXT NOT NULL REFERENCES Entities(id),
    entity_type TEXT NOT NULL,
    name TEXT NOT NULL,
    details TEXT NOT NULL,
    actor TEXT NOT NULL,
    timestamp DECIMAL(12,7) NOT NULL DEFAULT (EXTRACT(EPOCH FROM CURRENT_TIMESTAMP AT TIME ZONE 'UTC') / 86400),
    version INT NOT NULL,
    position BIGINT NOT NULL
);

CREATE TABLE IF NOT EXISTS Properties (
    name TEXT NOT NULL,
    value BIGINT NOT NULL
);

INSERT INTO Properties (name, value) SELECT 'next_position', 0
WHERE NOT EXISTS (SELECT 1 FROM Properties WHERE name = 'next_position');
