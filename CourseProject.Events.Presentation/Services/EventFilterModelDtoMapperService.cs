using CourseProject.Events.Application.Models;
using CourseProject.Events.Presentation.Interfaces;
using CourseProject.Events.Presentation.Models;

namespace CourseProject.Events.Presentation.Services
{
    public class EventFilterModelDtoMapperService : IEventFilterModelDtoMapperService
    {
        public EventFilterDto ModelToDto(EventFilterModel eventFilterModel)
        {
            EventFilterDto eventFilterDto = new()
            {
                Title = eventFilterModel.Title,
                From = eventFilterModel.From,
                To = eventFilterModel.To,
                Page = eventFilterModel.Page,
                PageSize = eventFilterModel.PageSize
            };

            return eventFilterDto;
        }

        public EventFilterModel DtoToModel(EventFilterDto eventFilterDto)
        {
            EventFilterModel eventFilterModel = new()
            {
                Title = eventFilterDto.Title,
                From = eventFilterDto.From,
                To = eventFilterDto.To,
                Page = eventFilterDto.Page,
                PageSize = eventFilterDto.PageSize
            };
            return eventFilterModel;
        }

    }
}
