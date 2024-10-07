IF OBJECT_ID('Entities') IS NULL
CREATE TABLE Entities (
    id NVARCHAR(256) NOT NULL PRIMARY KEY,
    type NVARCHAR(MAX) NOT NULL,
    version INT NOT NULL DEFAULT 0
);

IF OBJECT_ID('Events') IS NULL
CREATE TABLE Events (
    entity_id NVARCHAR(256) NOT NULL REFERENCES Entities (id),
    name NVARCHAR(MAX) NOT NULL,
    details NVARCHAR(MAX) NOT NULL,
    actor NVARCHAR(MAX) NOT NULL,
    -- SQL Server epoch is midnight Jan 1, 1900, which is 25,567 days before the Unix epoch
    timestamp DECIMAL(12,7) NOT NULL DEFAULT (CAST(CURRENT_TIMESTAMP AS DECIMAL(14,7)) - 25567),
    ordinal INT NOT NULL,
    position BIGINT NOT NULL
);
