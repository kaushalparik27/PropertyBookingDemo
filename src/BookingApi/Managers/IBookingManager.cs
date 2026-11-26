using BookingApi.Dtos;

namespace BookingApi.Managers;

public interface IBookingManager
{
    // Books seats on a slot for a renter and returns the confirmed booking.
    Task<BookingResponse> BookAsync(Guid slotId, CreateBookingRequest request);

    // Moves a booking to a new status if the business rules allow it.
    Task<BookingResponse> TransitionAsync(Guid bookingId, TransitionRequest request);

    // Returns the status change history of a booking.
    Task<List<HistoryResponse>> GetHistoryAsync(Guid bookingId);
}
