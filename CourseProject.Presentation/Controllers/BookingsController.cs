using CourseProject.Presentation.Interfaces;
using CourseProject.Presentation.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseProject.Presentation.Controllers
{
    [Route("bookings")]
    [ApiController]
    public class BookingsController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IBookingDtoMapperService _bookingDtoMapperService;

        public BookingsController(IBookingService bookingService, IBookingDtoMapperService bookingDtoMapperService)
        {
            _bookingService = bookingService;
            _bookingDtoMapperService = bookingDtoMapperService;
        }

        /// <summary>
        /// Get booking by ID
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Booking with the specified ID</returns>
        /// <response code="200">Booking received successfully</response>
        /// <response code="404">Booking not found</response>

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);

            if (booking == null)
            {
                return NotFound(); // 404 Not found
            }
            var bookingDto = _bookingDtoMapperService.EntityToDto(booking);
            return Ok(bookingDto); // 200 Ok
        }
    }
}
