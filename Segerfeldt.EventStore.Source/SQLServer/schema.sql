CREATE TABLE Entities (
    id NVARCHAR(128),
    [version] INT DEFAULT 0,

    PRIMARY KEY (id)
);

CREATE TABLE Events (
    entity NVARCHAR(128),
    [name] NVARCHAR(128),
    details NVARCHAR(MAX),
    actor NCHAR(36),
    [timestamp] INT DEFAULT CONVERT(INT, CURRENT_TIMESTAMP),
    [version] INT,
    position INT,

    FOREIGN KEY (entity) REFERENCES Entities (id)
);
