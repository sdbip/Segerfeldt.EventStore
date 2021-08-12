CREATE TABLE IF NOT EXISTS Entities (
    id TEXT,
    version INT,

    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS Events (
    entity TEXT,
    name TEXT,
    details TEXT,
    actor TEXT,
    timestamp LONG DEFAULT CURRENT_TIMESTAMP,
    version INT,
    position INT,

    FOREIGN KEY (entity) REFERENCES Entities (id)
);
