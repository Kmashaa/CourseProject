using CourseProject.Events.Application.Services;
using CourseProject.Events.Domain.Entities;
using CourseProject.Events.Application.Interfaces;
using CourseProject.Events.Application.Models;

namespace CourseProject.Events.Application.Services
{
    public class PaginatedResultDtoMapperService : IPaginatedResultDtoMapperService
    {
        private readonly IEventDtoMapperService _eventDtoMapperService;
        public PaginatedResultDtoMapperService(IEventDtoMapperService eventDtoMapperService)
        {
            _eventDtoMapperService = eventDtoMapperService;
        }

        public PaginatedResult DtoToEntity(PaginatedResultDto paginatedResultDto)
        {
            PaginatedResult paginatedResult = new()
            {
                TotalItems = paginatedResultDto.TotalItems,
                Events = paginatedResultDto.EventsDto.Select(o => _eventDtoMapperService.DtoToEntity(o)).ToList(),
                CurrentPage = paginatedResultDto.CurrentPage,
                NumOfItemsOnCurrentPage = paginatedResultDto.NumOfItemsOnCurrentPage
            };

            return paginatedResult;
        }

        public PaginatedResultDto EntityToDto(PaginatedResult paginatedResult)
        {
            PaginatedResultDto paginatedResultDto = new()
            {
                TotalItems = paginatedResult.TotalItems,
                EventsDto = paginatedResult.Events.Select(o => _eventDtoMapperService.EntityToDto(o)).ToList(),
                CurrentPage = paginatedResult.CurrentPage,
                NumOfItemsOnCurrentPage = paginatedResult.NumOfItemsOnCurrentPage
            };
            return paginatedResultDto;
        }

    }
}
