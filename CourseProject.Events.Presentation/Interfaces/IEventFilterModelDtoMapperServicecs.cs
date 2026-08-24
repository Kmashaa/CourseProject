using CourseProject.Events.Application.Models;
using CourseProject.Events.Presentation.Models;

namespace CourseProject.Events.Presentation.Interfaces
{
    public interface IEventFilterModelDtoMapperService
    {
        EventFilterDto ModelToDto(EventFilterModel eventFilterModel);

        EventFilterModel DtoToModel(EventFilterDto filterDto);

    }
}
