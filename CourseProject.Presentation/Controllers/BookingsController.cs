using CourseProject.Application.Interfaces;
using CourseProject.Application.Services;
using CourseProject.Domain.Entities;
using CourseProject.Presentation.Interfaces;
using CourseProject.Presentation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseProject.Presentation.Controllers
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
            var bookingDto = await _bookingService.GetBookingByIdAsync(id);

            if (bookingDto == null)
            {
                return NotFound(); // 404 Not found
            }
            var bookingModel = _bookingModelDtoMapperService.DtoToModel(bookingDto);
            return Ok(bookingModel); // 200 Ok
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
            var result = await _bookingService.CancelBookingAsync(id, Guid.NewGuid()); //TODO guid, role
            if (result == false)
            {
                return NotFound(); // 404 Not found
            }
            return NoContent(); // 204 No Content //TODO cancelled booking
        }
    }
}
