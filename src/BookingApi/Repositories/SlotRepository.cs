using BookingApi.Data;
using BookingApi.Dtos;
using BookingApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Repositories
{
    public class SlotRepository : ISlotRepository
    {
        private readonly BookingDbContext _db;

        public SlotRepository(BookingDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Slot slot)
        {
            _db.Slots.Add(slot);
            await _db.SaveChangesAsync();
        }

        public async Task<List<Slot>> ListAsync(SlotFilter filter)
        {
            return await new SlotQueryBuilder(_db.Slots.AsNoTracking())
                .ForProperty(filter.PropertyId)
                .StartingFrom(filter.From)
                .StartingUntil(filter.To)
                .Page(filter.Page, filter.PageSize)
                .Build()
                .ToListAsync();
        }
    }
}
