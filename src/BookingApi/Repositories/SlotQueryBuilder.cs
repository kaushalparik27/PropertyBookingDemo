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

        public SlotQueryBuilder ForProperty(Guid? propertyId)
        {
            if (propertyId != null)
                _query = _query.Where(s => s.PropertyId == propertyId);

            return this;
        }

        public SlotQueryBuilder StartingFrom(DateTimeOffset? from)
        {
            if (from != null)
                _query = _query.Where(s => s.StartsAt >= from);

            return this;
        }

        public SlotQueryBuilder StartingUntil(DateTimeOffset? to)
        {
            if (to != null)
                _query = _query.Where(s => s.StartsAt <= to);

            return this;
        }

        public SlotQueryBuilder Page(int page, int pageSize)
        {
            _query = _query
                .OrderBy(s => s.StartsAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return this;
        }

        public IQueryable<Slot> Build()
        {
            return _query;
        }
    }
}
