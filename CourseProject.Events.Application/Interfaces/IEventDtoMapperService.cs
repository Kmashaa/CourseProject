using CourseProject.Events.Application.Models;
using CourseProject.Events.Domain.Entities;

namespace CourseProject.Events.Application.Interfaces
{
    public interface IEventDtoMapperService
    {
        Event DtoToEntity(EventDto eventDto);

        EventDto EntityToDto(Event @event);
    }
}
