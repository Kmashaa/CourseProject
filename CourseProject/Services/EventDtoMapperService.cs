using CourseProject.Entities;
using CourseProject.Interfaces;
using CourseProject.Models;

namespace CourseProject.Services
{
    public class EventDtoMapperService: IEventDtoMapperService
    {
        public Event DtoToEntity(EventDto eventDto)
        {
            Event @event = new()
            {
                Id = eventDto.Id,
                Title = eventDto.Title,
                Description = eventDto.Description,
                StartAt = eventDto.StartAt,
                EndAt = eventDto.EndAt
            };
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
                EndAt = @event.EndAt
            };
            return eventDto;
        }

    }
}
