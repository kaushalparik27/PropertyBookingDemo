using BookingApi.Entities;

namespace BookingApi.Repositories;

public interface IBookingRepository
{
    // Returns the slot with this ID, or null if it doesn't exist.
    Task<Slot?> GetSlotByIdAsync(Guid slotId);

    // Returns the booking with its slot, or null if it doesn't exist.
    Task<Booking?> GetByIdAsync(Guid id);

    // Checks whether a booking with this ID exists.
    Task<bool> BookingExistsAsync(Guid id);

    // Checks whether the renter already has a confirmed booking on the slot.
    Task<bool> HasActiveBookingAsync(Guid slotId, string renterEmail);

    // Returns the history entries of a booking, oldest first.
    Task<List<BookingStateChange>> GetHistoryAsync(Guid bookingId);

    // Reserves the seats and saves the booking in one transaction. Returns false if the slot is full.
    Task<bool> AddAsync(Booking booking, BookingStateChange change);

    // Saves the new status and history entry, optionally giving the seats back, in one transaction.
    Task SaveTransitionAsync(Booking booking, BookingStateChange change, bool releaseSeats);
}
