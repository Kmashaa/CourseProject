using CourseProject.Application.Models;
using CourseProject.Application.Interfaces;
using CourseProject.Domain.Entities;

namespace CourseProject.Application.Services
{
    public class EventFilterDtoMapperService : IEventFilterDtoMapperService
    {
        public EventFilter DtoToEntity(EventFilterDto eventFilterDto)
        {
            EventFilter eventFilter = new()
            {
                Title = eventFilterDto.Title,
                From = eventFilterDto.From,
                To = eventFilterDto.To,
                Page = eventFilterDto.Page,
                PageSize = eventFilterDto.PageSize
            };

            return eventFilter;
        }

        public EventFilterDto EntityToDto(EventFilter eventFilter)
        {
            EventFilterDto eventFilterDto = new()
            {
                Title = eventFilter.Title,
                From = eventFilter.From,
                To = eventFilter.To,
                Page = eventFilter.Page,
                PageSize = eventFilter.PageSize
            };
            return eventFilterDto;
        }

    }
}
