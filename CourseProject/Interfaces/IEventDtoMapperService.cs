using CourseProject.Presentation.Entities;
using CourseProject.Presentation.Models;


namespace CourseProject.Presentation.Interfaces
{
    public interface IEventDtoMapperService
    {
        Event DtoToEntity(EventDto eventDto);

        EventDto EntityToDto(Event @event);
    }
}
