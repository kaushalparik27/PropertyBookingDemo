using BookingApi.Dtos;

namespace BookingApi.Managers;

public interface IBookingManager
{
    Task<BookingResponse> BookAsync(Guid slotId, CreateBookingRequest request);
    Task<BookingResponse> TransitionAsync(Guid bookingId, TransitionRequest request);
    Task<List<HistoryResponse>> GetHistoryAsync(Guid bookingId);
}
