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

        // Inserts a new slot into the database.
        public async Task AddAsync(Slot slot)
        {
            _db.Slots.Add(slot);
            await _db.SaveChangesAsync();
        }

        // Builds the filtered and paged query, then runs it without change tracking.
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
