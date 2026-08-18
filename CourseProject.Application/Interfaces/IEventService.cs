using CourseProject.Application.Models;

namespace CourseProject.Application.Interfaces
{
    public interface IEventService
    {
        Task<List<EventDto>?> GetAllEventsAsync();

        Task<EventDto?> GetEventByIdAsync(Guid id);

        Task<PaginatedResultDto> GetEventsAsync(EventFilterDto filterDto);

        Task<EventDto> CreateEventAsync(EventDto eventDto);

        Task<EventDto> UpdateEventAsync(EventDto eventDto);

        Task<bool> DeleteEventAsync(Guid? index);

    }
}
