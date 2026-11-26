using BookingApi.Dtos;
using BookingApi.Entities;
using BookingApi.Exceptions;
using BookingApi.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Managers
{
    public class BookingManager : IBookingManager
    {
        private readonly IBookingRepository _bookings;
        private readonly TimeProvider _clock;

        public BookingManager(IBookingRepository bookings, TimeProvider clock)
        {
            _bookings = bookings;
            _clock = clock;
        }

        // Checks the booking rules, then reserves the seats and saves a confirmed booking.
        public async Task<BookingResponse> BookAsync(Guid slotId, CreateBookingRequest request)
        {
            var slot = await _bookings.GetSlotByIdAsync(slotId)
                       ?? throw new NotFoundException("Slot not found.");

            var now = _clock.GetUtcNow();
            if (slot.StartsAt <= now)
                throw new ConflictException("The slot has already started.");

            var email = request.RenterEmail.Trim().ToLowerInvariant();
            if (await _bookings.HasActiveBookingAsync(slotId, email))
                throw new ConflictException("The renter already has an active booking for this slot.");

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                SlotId = slotId,
                RenterEmail = email,
                PartySize = request.PartySize,
                Status = BookingStatus.Confirmed,
                CreatedAt = now,
                UpdatedAt = now
            };

            var created = new BookingStateChange
            {
                BookingId = booking.Id,
                FromStatus = null,
                ToStatus = BookingStatus.Confirmed,
                Actor = Actor.Renter,
                Reason = "Booked",
                ChangedAt = now
            };

            try
            {
                if (!await _bookings.AddAsync(booking, created))
                    throw new ConflictException("Not enough seats left on this slot.");
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Two identical requests raced past the check above; the unique index stopped the second.
                throw new ConflictException("The renter already has an active booking for this slot.");
            }

            return ToResponse(booking);
        }

        // Validates a status change, saves it with a history entry, and frees seats on cancellation.
        public async Task<BookingResponse> TransitionAsync(Guid bookingId, TransitionRequest request)
        {
            var booking = await _bookings.GetByIdAsync(bookingId)
                          ?? throw new NotFoundException("Booking not found.");

            var to = request.To!.Value;
            var actor = request.Actor!.Value;
            var now = _clock.GetUtcNow();
            var slotStarted = now >= booking.Slot.StartsAt;

            if (booking.Status != BookingStatus.Confirmed)
                throw new ConflictException($"A {booking.Status} booking can't be changed.");

            switch (to)
            {
                case BookingStatus.Cancelled when slotStarted:
                    throw new ConflictException("A booking can't be cancelled after the slot has started.");

                case BookingStatus.Attended or BookingStatus.NoShow when actor != Actor.Owner:
                    throw new ConflictException("Only the owner can mark a booking as Attended or NoShow.");

                case BookingStatus.Attended or BookingStatus.NoShow when !slotStarted:
                    throw new ConflictException("A booking can't be marked as Attended or NoShow before the slot starts.");

                case BookingStatus.Confirmed:
                    throw new ConflictException("A booking can't be moved back to Confirmed.");
            }

            var change = new BookingStateChange
            {
                BookingId = booking.Id,
                FromStatus = booking.Status,
                ToStatus = to,
                Actor = actor,
                Reason = request.Reason,
                ChangedAt = now
            };

            booking.Status = to;
            booking.UpdatedAt = now;

            try
            {
                await _bookings.SaveTransitionAsync(booking, change, releaseSeats: to == BookingStatus.Cancelled);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("The booking was changed by someone else. Please retry.");
            }

            return ToResponse(booking);
        }

        // Returns a booking's history, or throws NotFoundException if the booking doesn't exist.
        public async Task<List<HistoryResponse>> GetHistoryAsync(Guid bookingId)
        {
            if (!await _bookings.BookingExistsAsync(bookingId))
                throw new NotFoundException("Booking not found.");

            var history = await _bookings.GetHistoryAsync(bookingId);
            return history.Select(c => new HistoryResponse
            {
                FromStatus = c.FromStatus,
                ToStatus = c.ToStatus,
                Actor = c.Actor,
                Reason = c.Reason,
                ChangedAt = c.ChangedAt
            }).ToList();
        }

        // Maps a Booking entity to the response DTO returned by the API.
        private static BookingResponse ToResponse(Booking booking)
        {
            return new BookingResponse
            {
                Id = booking.Id,
                SlotId = booking.SlotId,
                RenterEmail = booking.RenterEmail,
                PartySize = booking.PartySize,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt
            };
        }
    }
}
