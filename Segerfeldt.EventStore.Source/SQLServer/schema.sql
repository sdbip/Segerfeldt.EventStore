IF OBJECT_ID('Entities') IS NULL
CREATE TABLE Entities (
    id NVARCHAR(128),
    [version] INT DEFAULT 0,

    PRIMARY KEY (id)
);

IF OBJECT_ID('Events') IS NULL
CREATE TABLE Events (
    entity NVARCHAR(128),
    [name] NVARCHAR(128),
    details NVARCHAR(MAX),
    actor NVARCHAR(36),
    [timestamp] DATETIME DEFAULT CURRENT_TIMESTAMP,
    [version] INT,
    position BIGINT,

    FOREIGN KEY (entity) REFERENCES Entities (id)
);
