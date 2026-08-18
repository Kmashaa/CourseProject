using CourseProject.Application.Models;
using CourseProject.Presentation.Models;

namespace CourseProject.Presentation.Interfaces
{
    public interface IEventFilterModelDtoMapperService
    {
        EventFilterDto ModelToDto(EventFilterModel eventFilterModel);

        EventFilterModel DtoToModel(EventFilterDto filterDto);

    }
}
