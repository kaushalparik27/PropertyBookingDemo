using BookingApi.Entities;

namespace BookingApi.Repositories;

public interface IBookingRepository
{
    Task<Slot?> GetSlotByIdAsync(Guid slotId);
    Task<Booking?> GetByIdAsync(Guid id);
    Task<bool> BookingExistsAsync(Guid id);
    Task<bool> HasActiveBookingAsync(Guid slotId, string renterEmail);
    Task<List<BookingStateChange>> GetHistoryAsync(Guid bookingId);

    // Reserves the seats and saves the booking in one transaction. Returns false if the slot is full.
    Task<bool> AddAsync(Booking booking, BookingStateChange change);

    // Saves the new status and history entry, optionally giving the seats back, in one transaction.
    Task SaveTransitionAsync(Booking booking, BookingStateChange change, bool releaseSeats);
}
