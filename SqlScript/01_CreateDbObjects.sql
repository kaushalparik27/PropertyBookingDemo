IF DB_ID('BookingDb') IS NULL
    CREATE DATABASE BookingDb;
GO

USE BookingDb;
GO

IF OBJECT_ID('dbo.Slots', 'U') IS NULL
CREATE TABLE dbo.Slots
(
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Slots PRIMARY KEY,
    PropertyId      UNIQUEIDENTIFIER NOT NULL,
    StartsAt        DATETIMEOFFSET   NOT NULL,
    DurationMinutes INT              NOT NULL,
    Capacity        INT              NOT NULL,
    BookedSeats     INT              NOT NULL CONSTRAINT DF_Slots_BookedSeats DEFAULT 0,
    CreatedAt       DATETIMEOFFSET   NOT NULL CONSTRAINT DF_Slots_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    RowVersion      ROWVERSION       NOT NULL,
    CONSTRAINT CK_Slots_Duration CHECK (DurationMinutes BETWEEN 15 AND 120),
    CONSTRAINT CK_Slots_Capacity CHECK (Capacity BETWEEN 1 AND 20),
    CONSTRAINT CK_Slots_BookedSeats CHECK (BookedSeats >= 0 AND BookedSeats <= Capacity)
);
GO

IF OBJECT_ID('dbo.Bookings', 'U') IS NULL
CREATE TABLE dbo.Bookings
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Bookings PRIMARY KEY,
    SlotId      UNIQUEIDENTIFIER NOT NULL,
    RenterEmail NVARCHAR(320)    NOT NULL,
    PartySize   INT              NOT NULL,
    Status      NVARCHAR(20)     NOT NULL,
    CreatedAt   DATETIMEOFFSET   NOT NULL CONSTRAINT DF_Bookings_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt   DATETIMEOFFSET   NOT NULL CONSTRAINT DF_Bookings_UpdatedAt DEFAULT SYSDATETIMEOFFSET(),
    RowVersion  ROWVERSION       NOT NULL,
    CONSTRAINT FK_Bookings_Slots FOREIGN KEY (SlotId) REFERENCES dbo.Slots (Id),
    CONSTRAINT CK_Bookings_PartySize CHECK (PartySize BETWEEN 1 AND 6),
    CONSTRAINT CK_Bookings_Status CHECK (Status IN ('Confirmed', 'Cancelled', 'Attended', 'NoShow'))
);
GO

IF OBJECT_ID('dbo.BookingStateChanges', 'U') IS NULL
CREATE TABLE dbo.BookingStateChanges
(
    Id         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BookingStateChanges PRIMARY KEY,
    BookingId  UNIQUEIDENTIFIER     NOT NULL,
    FromStatus NVARCHAR(20)         NULL,
    ToStatus   NVARCHAR(20)         NOT NULL,
    Actor      NVARCHAR(10)         NOT NULL,
    Reason     NVARCHAR(500)        NULL,
    ChangedAt  DATETIMEOFFSET       NOT NULL CONSTRAINT DF_BookingStateChanges_ChangedAt DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT FK_BookingStateChanges_Bookings FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (Id),
    CONSTRAINT CK_BookingStateChanges_Actor CHECK (Actor IN ('owner', 'renter')),
    CONSTRAINT CK_BookingStateChanges_ToStatus CHECK (ToStatus IN ('Confirmed', 'Cancelled', 'Attended', 'NoShow'))
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Slots_PropertyId_StartsAt')
    CREATE INDEX IX_Slots_PropertyId_StartsAt ON dbo.Slots (PropertyId, StartsAt);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Slots_StartsAt')
    CREATE INDEX IX_Slots_StartsAt ON dbo.Slots (StartsAt);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bookings_SlotId')
    CREATE INDEX IX_Bookings_SlotId ON dbo.Bookings (SlotId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Bookings_SlotId_RenterEmail_Confirmed')
    CREATE UNIQUE INDEX UX_Bookings_SlotId_RenterEmail_Confirmed
        ON dbo.Bookings (SlotId, RenterEmail)
        WHERE Status = 'Confirmed';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BookingStateChanges_BookingId_ChangedAt')
    CREATE INDEX IX_BookingStateChanges_BookingId_ChangedAt ON dbo.BookingStateChanges (BookingId, ChangedAt);
GO
