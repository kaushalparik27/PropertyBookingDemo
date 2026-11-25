using BookingApi.Dtos;
using BookingApi.Entities;

namespace BookingApi.Repositories;

public interface ISlotRepository
{
    Task AddAsync(Slot slot);
    Task<List<Slot>> ListAsync(SlotFilter filter);
}
