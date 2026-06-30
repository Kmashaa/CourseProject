using CourseProject.Entities;
using CourseProject.Interfaces;
using CourseProject.Models;
using CourseProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseProject.Controllers
{
    [Route("events")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IEventDtoMapperService _eventDtoMapperService;

        public EventsController(IEventService eventService, IEventDtoMapperService eventDtoMapperService)
        {
            _eventService = eventService;
            _eventDtoMapperService = eventDtoMapperService;
        }

        /// <summary>
        /// Get all events
        /// </summary>
        /// <returns>List of all events</returns>
        /// <response code="200">Event list received successfully</response>
        [HttpGet]
        public IActionResult GetAll([FromQuery] EventFilter filter)
        {
            var events =_eventService.GetEvents(filter);
            PaginatedResultDto eventsDto = new()
            {
                TotalItems = events.TotalItems,
                CurrentPage = events.CurrentPage,
                NumOfItemsOnCurrentPage = events.NumOfItemsOnCurrentPage,
                EventsDto = events.Events.Select(o => _eventDtoMapperService.EntityToDto(o)).ToList()
            };
            return Ok(events); //200 Ok
        }

        /// <summary>
        /// Get event by ID
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Event with the specified ID</returns>
        /// <response code="200">Event received successfully</response>
        /// <response code="404">Event not found</response>

        [HttpGet("{id}")]
        public IActionResult GetById(Guid id)
        {
            var @event = _eventService.GetEventById(id);

            if (@event == null)
            {
                return NotFound(); // 404 Not found
            }
            var eventDto=_eventDtoMapperService.EntityToDto(@event);
            return Ok(eventDto); // 200 Ok
        }

        /// <summary>
        /// Create new event
        /// </summary>
        /// <returns>Created event</returns>
        /// <response code="201">Event created successfully</response>
        [HttpPost]
        public IActionResult Create([FromBody] EventDto eventDto)
        {
            var @event = _eventDtoMapperService.DtoToEntity(eventDto);
            _eventService.CreateEvent(@event);

            return CreatedAtAction(nameof(Create), new { id = @event.Id }, _eventDtoMapperService.EntityToDto(@event)); // 201 Created
        }

        /// <summary>
        /// Update an existing event
        /// </summary>
         /// <param name="id">Event ID</param>
        /// <returns>No data</returns>
        /// <response code="204">Event updated successfully. Returns no data</response>
        /// <response code="404">Event not found</response>


        [HttpPut("{id}")]
        public IActionResult Update(Guid id, [FromBody] EventDto eventDto)
        {
            eventDto.Id = id;
            var @event = _eventDtoMapperService.DtoToEntity(eventDto);

            _eventService.UpdateEvent(@event);

            return NoContent(); // 204 No Content
        }

        /// <summary>
        /// Delete event
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <response code="204">Event deleted successfully. Returns no data</response>
        /// <response code="404">Event not found</response>

        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            _eventService.DeleteEvent(id);
            return NoContent(); // 204 No Content
        }
    }
}
