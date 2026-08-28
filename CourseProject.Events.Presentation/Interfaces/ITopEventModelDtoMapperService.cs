using CourseProject.Events.Application.Models;
using CourseProject.Events.Presentation.Models;


namespace CourseProject.Events.Presentation.Interfaces
{
    public interface ITopEventModelDtoMapperService
    {
        TopEventDto ModelToDto(TopEventModel topEventModel);

        TopEventModel DtoToModel(TopEventDto topEventDto);
    }
}
