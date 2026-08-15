using CourseProject.Application.Models;
using CourseProject.Domain.Entities;


namespace CourseProject.Application.Interfaces
{
    public interface IEventFilterDtoMapperService
    {
        EventFilter DtoToEntity(EventFilterDto eventFilterModel);

        EventFilterDto EntityToDto(EventFilter filterDto);

    }
}
