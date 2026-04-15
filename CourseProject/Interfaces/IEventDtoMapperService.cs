using CourseProject.Entities;
using CourseProject.Models;


namespace CourseProject.Interfaces
{
    public interface IEventDtoMapperService
    {
        Event DtoToEntitie(EventDto eventDto);

        EventDto EntitieToDto(Event @event);
    }
}
