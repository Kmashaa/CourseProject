using CourseProject.Presentation.Entities;
using CourseProject.Presentation.Interfaces;
using CourseProject.Presentation.Models;

namespace CourseProject.Presentation.Services
{
    public class EventDtoMapperService : IEventDtoMapperService
    {
        public Event DtoToEntity(EventDto eventDto)
        {
            Event @event = new(eventDto.Id, eventDto.Title, DateTime.SpecifyKind(eventDto.StartAt, DateTimeKind.Utc), DateTime.SpecifyKind(eventDto.EndAt, DateTimeKind.Utc), (int)eventDto.TotalSeats, eventDto.Description);

            return @event;
        }

        public EventDto EntityToDto(Event @event)
        {
            EventDto eventDto = new()
            {
                Id = @event.Id,
                Title = @event.Title,
                Description = @event.Description,
                StartAt = @event.StartAt,
                EndAt = @event.EndAt,
                TotalSeats = @event.TotalSeats,
                AvailableSeats = @event.AvailableSeats
            };
            return eventDto;
        }

    }
}
