using CourseProject.Application.Models;
using CourseProject.Domain.Entities;

namespace CourseProject.Application.Services
{
    public interface IEventDtoMapperService
    {
        Event DtoToEntity(EventDto eventDto);

        EventDto EntityToDto(Event @event);
    }
}
