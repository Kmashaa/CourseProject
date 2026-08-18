using CourseProject.Presentation.Models;
using CourseProject.Application.Models;


namespace CourseProject.Presentation.Interfaces
{
    public interface IEventModelDtoMapperService
    {
        EventDto ModelToDto(EventModel eventModel);

        EventModel DtoToModel(EventDto eventDto);
    }
}
