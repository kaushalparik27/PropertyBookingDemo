using BookingApi.Data;
using BookingApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly BookingDbContext _db;

        public BookingRepository(BookingDbContext db)
        {
            _db = db;
        }

        // Reads a slot without change tracking, since it is only used for checks.
        public async Task<Slot?> GetSlotByIdAsync(Guid slotId)
        {
            return await _db.Slots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == slotId);
        }

        // Loads a booking and its slot with change tracking, so status updates can be saved.
        public async Task<Booking?> GetByIdAsync(Guid id)
        {
            return await _db.Bookings.Include(b => b.Slot).FirstOrDefaultAsync(b => b.Id == id);
        }

        // Checks that a booking exists without loading it.
        public async Task<bool> BookingExistsAsync(Guid id)
        {
            return await _db.Bookings.AnyAsync(b => b.Id == id);
        }

        // Looks for a confirmed booking by the same renter on the same slot.
        public async Task<bool> HasActiveBookingAsync(Guid slotId, string renterEmail)
        {
            return await _db.Bookings.AnyAsync(b => b.SlotId == slotId
                                                    && b.RenterEmail == renterEmail
                                                    && b.Status == BookingStatus.Confirmed);
        }

        // Reads a booking's history entries, oldest first.
        public async Task<List<BookingStateChange>> GetHistoryAsync(Guid bookingId)
        {
            return await _db.BookingStateChanges
                .AsNoTracking()
                .Where(c => c.BookingId == bookingId)
                .OrderBy(c => c.ChangedAt).ThenBy(c => c.Id)
                .ToListAsync();
        }

        // Reserves seats and saves the booking in one transaction; returns false if they don't fit.
        public async Task<bool> AddAsync(Booking booking, BookingStateChange change)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            // One atomic statement: the seats are only taken if they still fit within capacity.
            var reserved = await _db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE Slots SET BookedSeats = BookedSeats + {booking.PartySize}
                WHERE Id = {booking.SlotId} AND BookedSeats + {booking.PartySize} <= Capacity");

            if (reserved == 0) return false;

            _db.Bookings.Add(booking);
            _db.BookingStateChanges.Add(change);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
            return true;
        }

        // Saves a status change and its history in one transaction, releasing seats if requested.
        public async Task SaveTransitionAsync(Booking booking, BookingStateChange change, bool releaseSeats)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            if (releaseSeats)
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE Slots SET BookedSeats = BookedSeats - {booking.PartySize}
                    WHERE Id = {booking.SlotId}");
            }

            _db.BookingStateChanges.Add(change);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
        }
    }
}
