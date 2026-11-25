using BookingApi.Dtos;
using BookingApi.Entities;
using BookingApi.Exceptions;
using BookingApi.Repositories;

namespace BookingApi.Managers
{
    public class SlotManager : ISlotManager
    {
        private readonly ISlotRepository _slots;
        private readonly TimeProvider _clock;

        public SlotManager(ISlotRepository slots, TimeProvider clock)
        {
            _slots = slots;
            _clock = clock;
        }

        public async Task<SlotResponse> CreateAsync(CreateSlotRequest request)
        {
            var now = _clock.GetUtcNow();

            if (request.StartsAt!.Value <= now)
                throw new BadRequestException("startsAt must be in the future.");

            var slot = new Slot
            {
                Id = Guid.NewGuid(),
                PropertyId = request.PropertyId!.Value,
                StartsAt = request.StartsAt.Value,
                DurationMinutes = request.DurationMinutes,
                Capacity = request.Capacity,
                BookedSeats = 0,
                CreatedAt = now
            };

            await _slots.AddAsync(slot);
            return ToResponse(slot);
        }

        public async Task<List<SlotResponse>> GetAsync(SlotFilter filter)
        {
            var result = await _slots.ListAsync(filter);
            return result.Select(ToResponse).ToList();
        }

        private static SlotResponse ToResponse(Slot slot)
        {
            return new SlotResponse
            {
                Id = slot.Id,
                PropertyId = slot.PropertyId,
                StartsAt = slot.StartsAt,
                DurationMinutes = slot.DurationMinutes,
                Capacity = slot.Capacity,
                SeatsRemaining = slot.Capacity - slot.BookedSeats
            };
        }
    }
}
