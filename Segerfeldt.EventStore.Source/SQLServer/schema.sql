IF OBJECT_ID('Entities') IS NULL
CREATE TABLE Entities (
    id NVARCHAR(128),
    [type] NVARCHAR(128),
    [version] INT DEFAULT 0,

    PRIMARY KEY (id)
);

IF OBJECT_ID('Events') IS NULL
CREATE TABLE Events (
    entity NVARCHAR(128),
    [name] NVARCHAR(128),
    details NVARCHAR(MAX),
    actor NVARCHAR(36),
    -- SQL Server does not have a built-in method for julian day, so it is hard-coded here.
    -- SQL Server epoch is midnight Jan 1, 1900. That is Julian Day 2415020.5
    [timestamp] FLOAT(53) NOT NULL DEFAULT (CAST(CURRENT_TIMESTAMP AS FLOAT(53)) + 2415020.5),
    [version] INT,
    position BIGINT,

    FOREIGN KEY (entity) REFERENCES Entities (id)
);
