# Property Showing Booking API

An HTTP API where owners publish showing slots and renters book seats on them.
Built with C# / .NET 8, ASP.NET Core controllers, EF Core and **SQL Server (Docker)**.

## Run it

Prerequisites: .NET 8 SDK, Docker Desktop, SQL Server Management Studio (SSMS).

1. Set the SA password (it is not committed to the repository):
   - Copy `.env.example` to `.env` and choose a password (8+ characters, upper and lower case, digits and symbols).
   - Create `src/BookingApi/appsettings.Development.json` with the same password:
     ```json
     {
       "ConnectionStrings": {
         "BookingDb": "Server=localhost,1433;Database=BookingDb;User Id=sa;Password=<your password>;TrustServerCertificate=True"
       }
     }
     ```
2. Start SQL Server:
   ```
   docker compose up -d
   ```
3. Connect SSMS to the container:
   - Server name: `localhost,1433`
   - Authentication: SQL Server Authentication, login `sa`, password from `.env`
   - Tick **Trust server certificate** (under Options, Connection Properties)
4. In SSMS, open and execute the scripts in this order:
   1. `SqlScript/01_CreateDbObjects.sql` creates the `BookingDb` database, tables and indexes.
   2. `SqlScript/02_SeedTestData.sql` loads the test data.
5. Start the API:
   ```
   dotnet run --project src/BookingApi --launch-profile BookingApi
   ```
   Swagger UI: http://localhost:5080/swagger

The default connection string in `src/BookingApi/appsettings.json` has a placeholder password;
`appsettings.Development.json` overrides it. Seed IDs are fixed
(properties `1111...`, `2222...`, `3333...`; slots `A000...0001-0008`; bookings `B000...0001-0011`).

## Run the tests

```
dotnet test
```

- **Unit tests** (`BookingManagerTests`, `SlotManagerTests`) cover the booking and transition rules with mocked repositories and a fake clock.
- **Endpoint tests** (`ApiTests`) call the real API and the SQL Server container, so steps 1 to 4 above must be done first. They include a concurrency test: 20 parallel requests for a 5-seat slot give exactly 5 bookings and 15 conflicts.

## Database

SQL Server in Docker. Schema is in `SqlScript/01_CreateDbObjects.sql` (no EF migrations).

| Table | Purpose |
|---|---|
| `Slots` | Showing slots: property, start, duration, capacity, `BookedSeats` |
| `Bookings` | One row per booking, with `Status` and a `RowVersion` concurrency token |
| `BookingStateChanges` | History: from/to status, actor, reason, timestamp |

Key constraints: CHECKs for every range in the spec, `BookedSeats <= Capacity`, and a filtered unique index on
`Bookings(SlotId, RenterEmail) WHERE Status = 'Confirmed'` (one active booking per renter per slot).

## Structure

```
Controllers/   HTTP only: bind the request, call a manager, return a DTO
Managers/      Business rules and mapping entities to DTOs
Repositories/  Data access only (EF Core and the atomic seat updates)
Entities/      EF Core entities (never returned from the API)
Dtos/          Request and response models, with validation attributes
Exceptions/    Bad request / not found / conflict, mapped to 400 / 404 / 409 by one filter
```

## Design decisions

- **Controllers, managers, repositories.** Plain classes injected through DI, with no MediatR. Easy to follow and to unit test.
- **No overbooking.** Booking runs one atomic statement in a transaction:
  `UPDATE Slots SET BookedSeats = BookedSeats + @n WHERE Id = @id AND BookedSeats + @n <= Capacity`.
  If no row is updated the slot is full and the API returns 409. The row lock serializes competing requests, and a CHECK constraint is a second safety net.
  The seat update lives in `BookingRepository`, since it must share a transaction with the booking insert.
- **Cancelling frees seats** in the same transaction as the status change. Attended and NoShow keep their seats.
- **One active booking per renter per slot.** A pre-check gives a clear message. The unique index catches two identical requests racing each other.
  A renter can rebook a slot after cancelling, because only Confirmed bookings count.
- **State rules** live in `BookingManager.TransitionAsync`:
  Confirmed to Cancelled (either actor, before the slot starts), Confirmed to Attended or NoShow (owner only, after the slot starts). Everything else is 409.
- **Concurrent transitions** on one booking are guarded by `Bookings.RowVersion`. The loser gets 409.
- **Time** comes from an injected `TimeProvider`, so before/after-start rules are testable.
- **Errors:** DTO validation gives 400 automatically. Business rule violations are exceptions turned into 400, 404 or 409 by `ApiExceptionFilter`.
- **No Properties table.** `PropertyId` is treated as an external reference, because the spec has no property data or endpoints.
- **Booking a slot that has already started** returns 409. The spec doesn't say this, so it's my assumption.

## What I'd do with more time

- Properties table and real owner identity, so only the property's owner can act as `owner`.
- Authentication is out of scope, so `actor` is trusted from the request body.
- Move the connection string and password out of `appsettings.json` into user secrets.

## AI use

I used Claude Code (Claude Sonnet 5.5) as a pair programmer for this project.

- **Used it for:** reading the assignment and listing the tech stack, comparing mediator libraries with plain services, drafting the table design, writing the SQL scripts, scaffolding the solution, and writing the unit and endpoint tests.
- **What I decided myself:** controller-style monolith, no MediatR, repositories plus managers, SQL Server in Docker, the Builder Pattern for slot search (`SlotQueryBuilder`), and the structure and rules for `Program.cs`.
- **What it got wrong, and how it was fixed:**
  - The history row was inserted before its booking, because EF didn't know about the foreign key. Found by running the tests. Fixed by declaring the relationship in `BookingDbContext`.
  - Its first duplicate-booking error handling caught every `DbUpdateException`, which hid that bug as a misleading 409. Narrowed to SQL duplicate-key errors only.
  - The first SQL script run failed because `sqlcmd` needs `-I` for filtered indexes.
- **Checked by hand:** the transaction and atomic-update logic, and what each test proves, which I can explain in the follow-up interview.
