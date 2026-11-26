using BookingApi.Dtos;

namespace BookingApi.Managers;

public interface ISlotManager
{
    // Creates a new showing slot that starts in the future.
    Task<SlotResponse> CreateAsync(CreateSlotRequest request);

    // Returns one page of slots that match the filter.
    Task<List<SlotResponse>> GetAsync(SlotFilter filter);
}
