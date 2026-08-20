using CourseProject.Events.Application.Models;
using CourseProject.Events.Domain.Entities;


namespace CourseProject.Events.Application.Interfaces
{
    public interface IEventFilterDtoMapperService
    {
        EventFilter DtoToEntity(EventFilterDto eventFilterModel);

        EventFilterDto EntityToDto(EventFilter filterDto);

    }
}
