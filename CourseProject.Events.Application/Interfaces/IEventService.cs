using CourseProject.Events.Application.Models;

namespace CourseProject.Events.Application.Interfaces
{
    public interface IEventService
    {
        Task<EventDto?> GetEventByIdAsync(Guid id);

        Task<PaginatedResultDto> GetEventsAsync(EventFilterDto filterDto);

        Task<EventDto> CreateEventAsync(EventDto eventDto);

        Task<EventDto> UpdateEventAsync(EventDto eventDto);

        Task<bool> DeleteEventAsync(Guid? index);

    }
}
