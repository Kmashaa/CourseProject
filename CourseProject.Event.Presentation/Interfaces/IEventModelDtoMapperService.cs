using CourseProject.Events.Application.Models;
using CourseProject.Events.Presentation.Models;


namespace CourseProject.Events.Presentation.Interfaces
{
    public interface IEventModelDtoMapperService
    {
        EventDto ModelToDto(EventModel eventModel);

        EventModel DtoToModel(EventDto eventDto);
    }
}
