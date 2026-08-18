using CourseProject.Application.Interfaces;
//using CourseProject.Presentation.Exceptions;
using CourseProject.Presentation.Interfaces;
using CourseProject.Presentation.Models;
using Microsoft.AspNetCore.Mvc;

namespace CourseProject.Presentation.Controllers
{
    [Route("events")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IBookingService _bookingService;
        private readonly IEventModelDtoMapperService _eventModelDtoMapperService;
        private readonly IBookingModelDtoMapperService _bookingModelDtoMapperService;
        private readonly IEventFilterModelDtoMapperService _eventFilterModelDtoMapperService;

        public EventsController(IEventService eventService, IBookingService bookingService, IEventModelDtoMapperService eventModelDtoMapperService, IBookingModelDtoMapperService bookingModelDtoMapperService, IEventFilterModelDtoMapperService eventFilterModelDtoMapperService)
        {
            _eventService = eventService;
            _bookingService = bookingService;
            _eventModelDtoMapperService = eventModelDtoMapperService;
            _bookingModelDtoMapperService = bookingModelDtoMapperService;
            _eventFilterModelDtoMapperService = eventFilterModelDtoMapperService;


        }

        /// <summary>
        /// Get all events
        /// </summary>
        /// <returns>List of all events</returns>
        /// <response code="200">Event list received successfully</response>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] EventFilterModel eventFilterModel)
        {
            var eventFilterDto = _eventFilterModelDtoMapperService.ModelToDto(eventFilterModel);

            var eventsDto = await _eventService.GetEventsAsync(eventFilterDto);
            PaginatedResultModel eventsModel = new()
            {
                TotalItems = eventsDto.TotalItems,
                CurrentPage = eventsDto.CurrentPage,
                NumOfItemsOnCurrentPage = eventsDto.NumOfItemsOnCurrentPage,
                EventsModel = eventsDto.EventsDto.Select(o => _eventModelDtoMapperService.DtoToModel(o)).ToList()
            };
            return Ok(eventsModel); //200 Ok
        }

        /// <summary>
        /// Get event by ID
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Event with the specified ID</returns>
        /// <response code="200">Event received successfully</response>
        /// <response code="404">Event not found</response>

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var eventDto = await _eventService.GetEventByIdAsync(id);

            if (eventDto == null)
            {
                return NotFound(); // 404 Not found
            }
            var eventModel = _eventModelDtoMapperService.DtoToModel(eventDto);
            return Ok(eventModel); // 200 Ok
        }

        /// <summary>
        /// Create new event
        /// </summary>
        /// <returns>Created event</returns>
        /// <response code="201">Event created successfully</response>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EventModel eventModel)
        {
            var eventDto = _eventModelDtoMapperService.ModelToDto(eventModel);
            await _eventService.CreateEventAsync(eventDto);

            return CreatedAtAction(nameof(GetById), new { id = eventDto.Id }, _eventModelDtoMapperService.DtoToModel(eventDto)); // 201 Created
        }

        /// <summary>
        /// Update an existing event
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>No data</returns>
        /// <response code="204">Event updated successfully. Returns no data</response>
        /// <response code="404">Event not found</response>


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EventModel eventModel)
        {
            eventModel.Id = id;
            var eventDto = _eventModelDtoMapperService.ModelToDto(eventModel);

            await _eventService.UpdateEventAsync(eventDto);

            return NoContent(); // 204 No Content
        }

        /// <summary>
        /// Delete event
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <response code="204">Event deleted successfully. Returns no data</response>
        /// <response code="404">Event not found</response>

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _eventService.DeleteEventAsync(id);
            if (result == false)
            {
                return NotFound(); // 404 Not found
            }
            return NoContent(); // 204 No Content
        }

        /// <summary>
        /// Book event
        /// </summary>
        /// <returns>Booking detailst</returns>
        /// <response code="202">Bookings was accepted successfully</response>
        /// <response code="404">Event was not found</response>
        /// <response code="409">Event no available seats for the event</response>

        [HttpPost("{id}/book")]
        public async Task<IActionResult> BookEvent(Guid id)
        {
            var bookingModel = _bookingModelDtoMapperService.DtoToModel(await _bookingService.CreateBookingAsync(id, Guid.NewGuid())); //TODO: user's guid
            return AcceptedAtAction(nameof(BookingsController.GetById), "Bookings", new { id = bookingModel.Id }, bookingModel);
        }
    }
}
