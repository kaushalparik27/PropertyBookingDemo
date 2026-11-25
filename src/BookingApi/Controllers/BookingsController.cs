using BookingApi.Dtos;
using BookingApi.Managers;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers
{
    [ApiController]
    [Route("bookings")]
    [Produces("application/json")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingManager _bookingManager;

        public BookingsController(IBookingManager bookingManager)
        {
            _bookingManager = bookingManager;
        }

        /// <summary>
        /// Moves a booking to a new status.
        /// Confirmed to Cancelled: renter or owner, before the slot starts (frees the seats).
        /// Confirmed to Attended or NoShow: owner only, after the slot starts.
        /// Cancelled, Attended and NoShow are final.
        /// </summary>
        /// <param name="id">The booking to change.</param>
        /// <param name="request">Target status (to), who is acting (owner or renter) and an optional reason.</param>
        /// <returns>The booking with its new status.</returns>
        /// <response code="200">The transition was applied and recorded in the history.</response>
        /// <response code="400">The request is invalid, for example the actor is not owner or renter.</response>
        /// <response code="404">The booking does not exist.</response>
        /// <response code="409">The transition is not allowed by the booking rules.</response>
        [HttpPost("{id:guid}/transitions")]
        [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> TransitionBooking(Guid id, [FromBody] TransitionRequest request)
        {
            BookingResponse booking = await _bookingManager.TransitionAsync(id, request);
            return Ok(booking);
        }

        /// <summary>
        /// Gets every state change of a booking, oldest first.
        /// </summary>
        /// <param name="id">The booking whose history to return.</param>
        /// <returns>Each change with its from and to status, actor, reason and timestamp.</returns>
        /// <response code="200">The booking history.</response>
        /// <response code="404">The booking does not exist.</response>
        [HttpGet("{id:guid}/history")]
        [ProducesResponseType(typeof(List<HistoryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBookingHistory(Guid id)
        {
            List<HistoryResponse> history = await _bookingManager.GetHistoryAsync(id);
            return Ok(history);
        }
    }
}
