USE BookingDb;
GO

DELETE FROM dbo.BookingStateChanges;
DELETE FROM dbo.Bookings;
DELETE FROM dbo.Slots;
GO

DECLARE @Today DATETIMEOFFSET = CAST(CAST(SYSUTCDATETIME() AS DATE) AS DATETIMEOFFSET);

DECLARE @PropertyA UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @PropertyB UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @PropertyC UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';

-- Slots: upcoming (S1-S6), past (S7-S8)
INSERT INTO dbo.Slots (Id, PropertyId, StartsAt, DurationMinutes, Capacity, BookedSeats, CreatedAt)
VALUES
('A0000000-0000-0000-0000-000000000001', @PropertyA, DATEADD(HOUR, 34, @Today),  30,  5, 0, DATEADD(DAY, -5, @Today)),
('A0000000-0000-0000-0000-000000000002', @PropertyA, DATEADD(HOUR, 58, @Today),  60, 10, 0, DATEADD(DAY, -5, @Today)),
('A0000000-0000-0000-0000-000000000003', @PropertyA, DATEADD(HOUR, 82, @Today),  45,  1, 0, DATEADD(DAY, -5, @Today)),
('A0000000-0000-0000-0000-000000000004', @PropertyB, DATEADD(HOUR, 36, @Today),  30, 20, 0, DATEADD(DAY, -4, @Today)),
('A0000000-0000-0000-0000-000000000005', @PropertyB, DATEADD(HOUR, 130, @Today), 120, 3, 0, DATEADD(DAY, -4, @Today)),
('A0000000-0000-0000-0000-000000000006', @PropertyC, DATEADD(HOUR, 178, @Today), 15,  2, 0, DATEADD(DAY, -3, @Today)),
('A0000000-0000-0000-0000-000000000007', @PropertyA, DATEADD(HOUR, -38, @Today), 30,  6, 0, DATEADD(DAY, -7, @Today)),
('A0000000-0000-0000-0000-000000000008', @PropertyB, DATEADD(HOUR, -14, @Today), 60,  8, 0, DATEADD(DAY, -7, @Today));

-- Bookings
INSERT INTO dbo.Bookings (Id, SlotId, RenterEmail, PartySize, Status, CreatedAt, UpdatedAt)
VALUES
-- S1: 4 of 5 seats taken
('B0000000-0000-0000-0000-000000000001', 'A0000000-0000-0000-0000-000000000001', 'alice@example.com', 2, 'Confirmed', DATEADD(DAY, -2, @Today), DATEADD(DAY, -2, @Today)),
('B0000000-0000-0000-0000-000000000002', 'A0000000-0000-0000-0000-000000000001', 'bob@example.com',   2, 'Confirmed', DATEADD(DAY, -2, @Today), DATEADD(DAY, -2, @Today)),
-- S3: full (capacity 1)
('B0000000-0000-0000-0000-000000000003', 'A0000000-0000-0000-0000-000000000003', 'carol@example.com', 1, 'Confirmed', DATEADD(DAY, -1, @Today), DATEADD(DAY, -1, @Today)),
-- S4: one confirmed, one cancelled
('B0000000-0000-0000-0000-000000000004', 'A0000000-0000-0000-0000-000000000004', 'dave@example.com',  6, 'Confirmed', DATEADD(DAY, -1, @Today), DATEADD(DAY, -1, @Today)),
('B0000000-0000-0000-0000-000000000005', 'A0000000-0000-0000-0000-000000000004', 'erin@example.com',  3, 'Cancelled', DATEADD(DAY, -2, @Today), DATEADD(DAY, -1, @Today)),
-- S6: full (capacity 2)
('B0000000-0000-0000-0000-000000000006', 'A0000000-0000-0000-0000-000000000006', 'frank@example.com', 2, 'Confirmed', DATEADD(DAY, -1, @Today), DATEADD(DAY, -1, @Today)),
-- S7 (past): attended, no-show, cancelled
('B0000000-0000-0000-0000-000000000007', 'A0000000-0000-0000-0000-000000000007', 'grace@example.com', 2, 'Attended',  DATEADD(DAY, -6, @Today), DATEADD(HOUR, -36, @Today)),
('B0000000-0000-0000-0000-000000000008', 'A0000000-0000-0000-0000-000000000007', 'heidi@example.com', 3, 'NoShow',    DATEADD(DAY, -6, @Today), DATEADD(HOUR, -36, @Today)),
('B0000000-0000-0000-0000-000000000009', 'A0000000-0000-0000-0000-000000000007', 'ivan@example.com',  1, 'Cancelled', DATEADD(DAY, -6, @Today), DATEADD(DAY, -3, @Today)),
-- S8 (past): one still Confirmed so the owner can mark Attended or NoShow
('B0000000-0000-0000-0000-000000000010', 'A0000000-0000-0000-0000-000000000008', 'judy@example.com',  2, 'Confirmed', DATEADD(DAY, -5, @Today), DATEADD(DAY, -5, @Today)),
('B0000000-0000-0000-0000-000000000011', 'A0000000-0000-0000-0000-000000000008', 'karl@example.com',  4, 'Attended',  DATEADD(DAY, -5, @Today), DATEADD(HOUR, -12, @Today));

-- History
INSERT INTO dbo.BookingStateChanges (BookingId, FromStatus, ToStatus, Actor, Reason, ChangedAt)
VALUES
('B0000000-0000-0000-0000-000000000001', NULL, 'Confirmed', 'renter', 'Booked', DATEADD(DAY, -2, @Today)),
('B0000000-0000-0000-0000-000000000002', NULL, 'Confirmed', 'renter', 'Booked', DATEADD(DAY, -2, @Today)),
('B0000000-0000-0000-0000-000000000003', NULL, 'Confirmed', 'renter', 'Booked', DATEADD(DAY, -1, @Today)),
('B0000000-0000-0000-0000-000000000004', NULL, 'Confirmed', 'renter', 'Booked', DATEADD(DAY, -1, @Today)),
('B0000000-0000-0000-0000-000000000005', NULL, 'Confirmed', 'renter', 'Booked', DATEADD(DAY, -2, @Today)),
('B0000000-0000-0000-0000-000000000005', 'Confirmed', 'Cancelled', 'renter', 'Change of plans', DATEADD(DAY, -1, @Today)),
('B0000000-0000-0000-0000-000000000006', NULL, 'Confirmed', 'renter', 'Booked', DATEADD(DAY, -1, @Today)),
('B0000000-0000-0000-0000-000000000007', NULL, 'Confirmed', 'renter', 'Booked', DATEADD(DAY, -6, @Today)),
('B0000000-0000-0000-0000-000000000007', 'Confirmed', 'Attended', 'owner', 'Showed up on time', DATEADD(HOUR, -36, @Today)),
('B0000000-0000-0000-0000-000000000008', NULL, 'Confirmed', 'renter', 'Booked', DATEADD(DAY, -6, @Today)),
('B0000000-0000-0000-0000-000000000008', 'Confirmed', 'NoShow', 'owner', 'Did not arrive', DATEADD(HOUR, -36, @Today)),
('B0000000-0000-0000-0000-000000000009', NULL, 'Confirmed', 'renter', 'Booked', DATEADD(DAY, -6, @Today)),
('B0000000-0000-0000-0000-000000000009', 'Confirmed', 'Cancelled', 'owner', 'Owner rescheduling', DATEADD(DAY, -3, @Today)),
('B0000000-0000-0000-0000-000000000010', NULL, 'Confirmed', 'renter', 'Booked', DATEADD(DAY, -5, @Today)),
('B0000000-0000-0000-0000-000000000011', NULL, 'Confirmed', 'renter', 'Booked', DATEADD(DAY, -5, @Today)),
('B0000000-0000-0000-0000-000000000011', 'Confirmed', 'Attended', 'owner', 'Showed up on time', DATEADD(HOUR, -12, @Today));

-- Extra upcoming slots for pagination and date-range filtering
DECLARE @i INT = 1;
WHILE @i <= 25
BEGIN
    INSERT INTO dbo.Slots (Id, PropertyId, StartsAt, DurationMinutes, Capacity, BookedSeats, CreatedAt)
    VALUES (NEWID(), @PropertyC, DATEADD(HOUR, 200 + @i * 6, @Today), 30, 4, 0, DATEADD(DAY, -1, @Today));
    SET @i += 1;
END;

-- Seats stay held by every booking except Cancelled
UPDATE s
SET BookedSeats = ISNULL(b.Seats, 0)
FROM dbo.Slots s
LEFT JOIN (
    SELECT SlotId, SUM(PartySize) AS Seats
    FROM dbo.Bookings
    WHERE Status <> 'Cancelled'
    GROUP BY SlotId
) b ON b.SlotId = s.Id;
GO
