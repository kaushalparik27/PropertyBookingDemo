using BookingApi.Dtos;
using BookingApi.Entities;

namespace BookingApi.Repositories;

public interface ISlotRepository
{
    // Saves a new slot to the database.
    Task AddAsync(Slot slot);

    // Returns one page of slots that match the filter, ordered by start time.
    Task<List<Slot>> ListAsync(SlotFilter filter);
}
