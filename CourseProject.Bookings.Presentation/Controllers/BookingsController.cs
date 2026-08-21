using CourseProject.Bookings.Application.Interfaces;
using CourseProject.Bookings.Application.Services;
using CourseProject.Bookings.Presentation.Interfaces;
using CourseProject.Bookings.Domain.Entities;
using CourseProject.Bookings.Presentation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CourseProject.Bookings.Presentation.Controllers
{
    [Route("bookings")]
    [ApiController]
    public class BookingsController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IBookingModelDtoMapperService _bookingModelDtoMapperService;

        public BookingsController(IBookingService bookingService, IBookingModelDtoMapperService bookingModelDtoMapperService)
        {
            _bookingService = bookingService;
            _bookingModelDtoMapperService = bookingModelDtoMapperService;
        }

        /// <summary>
        /// Get booking by ID
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Booking with the specified ID</returns>
        /// <response code="200">Booking received successfully</response>
        /// <response code="404">Booking not found</response>

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = User.FindFirst(ClaimTypes.Role);


            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId) && userRoleClaim != null)
            {
                string userRole = userRoleClaim.Value;

                var bookingDto = await _bookingService.GetBookingByIdAsync(id, userId, userRole);

                if (bookingDto == null)
                {
                    return NotFound(); // 404 Not found
                }
                var bookingModel = _bookingModelDtoMapperService.DtoToModel(bookingDto);
                return Ok(bookingModel); // 200 Ok
            }
            else
            {
                return Unauthorized("Неверный формат ID пользователя в токене");
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        /// <response code=""></response>
        /// <response code=""></response>

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = User.FindFirst(ClaimTypes.Role);


            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId) && userRoleClaim != null)
            {
                string userRole = userRoleClaim.Value;

                var result = await _bookingService.CancelBookingAsync(id, userId, userRole);
                if (result == null)
                {
                    return NotFound(); // 404 Not found
                }
                return NoContent(); // 204 No Content 
            }
            else
            {
                return Unauthorized("Неверный формат ID пользователя в токене");
            }
        }

        /// <summary>
        /// Book event
        /// </summary>
        /// <returns>Booking details</returns>
        /// <response code="202">Bookings was accepted successfully</response>
        /// <response code="404">Event was not found</response>
        /// <response code="409">Event no available seats for the event</response>

        [Authorize]
        [HttpPost("{eventId}/book")]
        public async Task<IActionResult> BookEvent(Guid eventId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                var bookingModel = _bookingModelDtoMapperService.DtoToModel(await _bookingService.CreateBookingAsync(eventId, userId));
                return AcceptedAtAction(nameof(BookingsController.GetById), "Bookings", new { id = bookingModel.Id }, bookingModel);
            }
            else
            {
                return Unauthorized("Неверный формат ID пользователя в токене");
            }
        }
    }
}
