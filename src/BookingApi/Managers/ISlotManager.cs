using BookingApi.Dtos;

namespace BookingApi.Managers;

public interface ISlotManager
{
    Task<SlotResponse> CreateAsync(CreateSlotRequest request);
    Task<List<SlotResponse>> GetAsync(SlotFilter filter);
}
