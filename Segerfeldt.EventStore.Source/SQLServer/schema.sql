IF OBJECT_ID('Entities') IS NULL
CREATE TABLE Entities (
    id NVARCHAR(256) NOT NULL PRIMARY KEY,
    [type] NVARCHAR(MAX) NOT NULL,
    [version] INT NOT NULL DEFAULT 0
);

IF OBJECT_ID('Events') IS NULL
CREATE TABLE Events (
    entityId NVARCHAR(256) NOT NULL REFERENCES Entities (id),
    entityType NVARCHAR(MAX) NOT NULL,
    [name] NVARCHAR(MAX) NOT NULL,
    details NVARCHAR(MAX) NOT NULL,
    actor NVARCHAR(MAX) NOT NULL,
    -- SQL Server does not have a built-in method for julian day, so it is hard-coded here.
    -- SQL Server epoch is midnight Jan 1, 1900. That is Julian Day 2415020.5
    [timestamp] FLOAT(53) NOT NULL DEFAULT (CAST(CURRENT_TIMESTAMP AS FLOAT(53)) + 2415020.5),
    [version] INT NOT NULL,
    position BIGINT NOT NULL
);
