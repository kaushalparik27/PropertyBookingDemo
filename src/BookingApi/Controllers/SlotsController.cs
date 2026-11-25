using BookingApi.Dtos;
using BookingApi.Managers;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers
{
    [ApiController]
    [Route("slots")]
    [Produces("application/json")]
    public class SlotsController : ControllerBase
    {
        private readonly ISlotManager _slotManager;
        private readonly IBookingManager _bookingManager;

        public SlotsController(ISlotManager slotManager, IBookingManager bookingManager)
        {
            _slotManager = slotManager;
            _bookingManager = bookingManager;
        }

        /// <summary>
        /// Creates a showing slot for a property.
        /// </summary>
        /// <param name="request">Property, start time (must be in the future), duration (15-120 minutes) and capacity (1-20).</param>
        /// <returns>The created slot, with all seats still free.</returns>
        /// <response code="201">The slot was created.</response>
        /// <response code="400">The request is invalid, for example the start time is in the past.</response>
        [HttpPost]
        [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateSlot([FromBody] CreateSlotRequest request)
        {
            SlotResponse slot = await _slotManager.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, slot);
        }

        /// <summary>
        /// Lists slots ordered by start time, with the seats remaining on each.
        /// </summary>
        /// <param name="filter">Optional filters: propertyId, from, to. Paging: page (default 1) and pageSize (default 20, maximum 100).</param>
        /// <returns>The matching slots for the requested page. The list is empty when nothing matches.</returns>
        /// <response code="200">The page of slots.</response>
        /// <response code="400">A filter value is invalid, for example pageSize is above 100.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<SlotResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSlots([FromQuery] SlotFilter filter)
        {
            List<SlotResponse> slots = await _slotManager.GetAsync(filter);
            return Ok(slots);
        }

        /// <summary>
        /// Books seats on a slot for a renter's party. The booking starts as Confirmed.
        /// </summary>
        /// <param name="id">The slot to book.</param>
        /// <param name="request">Renter email and party size (1-6).</param>
        /// <returns>The created booking.</returns>
        /// <response code="201">The booking was created and the seats are reserved.</response>
        /// <response code="400">The request is invalid, for example the email is malformed.</response>
        /// <response code="404">The slot does not exist.</response>
        /// <response code="409">The slot has started, has too few seats left, or the renter already has an active booking on it.</response>
        [HttpPost("{id:guid}/bookings")]
        [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> BookSlot(Guid id, [FromBody] CreateBookingRequest request)
        {
            BookingResponse booking = await _bookingManager.BookAsync(id, request);
            return StatusCode(StatusCodes.Status201Created, booking);
        }
    }
}
