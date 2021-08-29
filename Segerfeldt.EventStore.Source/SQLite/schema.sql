CREATE TABLE IF NOT EXISTS Entities (
    id TEXT,
    type TEXT,
    version INT,

    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS Events (
    entity TEXT,
    name TEXT,
    details TEXT,
    actor TEXT,
    timestamp TIMESTAMP,
    version INT,
    position BIGINT,

    FOREIGN KEY (entity) REFERENCES Entities (id)
);
