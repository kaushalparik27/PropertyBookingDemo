using BookingApi.Entities;

namespace BookingApi.Repositories
{
    public class SlotQueryBuilder
    {
        private IQueryable<Slot> _query;

        public SlotQueryBuilder(IQueryable<Slot> query)
        {
            _query = query;
        }

        // Filters by property, if a property ID is given.
        public SlotQueryBuilder ForProperty(Guid? propertyId)
        {
            if (propertyId != null)
                _query = _query.Where(s => s.PropertyId == propertyId);

            return this;
        }

        // Keeps slots starting at or after the given time, if one is given.
        public SlotQueryBuilder StartingFrom(DateTimeOffset? from)
        {
            if (from != null)
                _query = _query.Where(s => s.StartsAt >= from);

            return this;
        }

        // Keeps slots starting at or before the given time, if one is given.
        public SlotQueryBuilder StartingUntil(DateTimeOffset? to)
        {
            if (to != null)
                _query = _query.Where(s => s.StartsAt <= to);

            return this;
        }

        // Orders by start time and takes only the requested page.
        public SlotQueryBuilder Page(int page, int pageSize)
        {
            _query = _query
                .OrderBy(s => s.StartsAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return this;
        }

        // Returns the finished query; it hasn't run against the database yet.
        public IQueryable<Slot> Build()
        {
            return _query;
        }
    }
}
