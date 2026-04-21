using CourseProject.Entities;
using CourseProject.Models;


namespace CourseProject.Interfaces
{
    public interface IEventDtoMapperService
    {
        Event DtoToEntity(EventDto eventDto);

        EventDto EntityToDto(Event @event);
    }
}
