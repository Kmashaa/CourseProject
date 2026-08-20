using CourseProject.Events.Domain.Entities;
using CourseProject.Events.Application.Interfaces;
using CourseProject.Events.Application.Models;

namespace CourseProject.Events.Application.Services
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
            EventDto eventDto = new(@event.Id, @event.Title, @event.StartAt, @event.EndAt, @event.TotalSeats, @event.AvailableSeats, @event.Description) { };
            return eventDto;
        }

    }
}
