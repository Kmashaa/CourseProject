using CourseProject.Entities;
using CourseProject.Models;

namespace CourseProject.Interfaces
{
    public interface IEventService
    {
        Task<List<Event>?> GetAllEventsAsync();

        Task<Event?> GetEventByIdAsync(Guid id);

        Task<PaginatedResult> GetEventsAsync(EventFilter filter);

        Task<Event> CreateEventAsync(Event @event);

        Task<Event> UpdateEventAsync(Event @event);

        Task DeleteEventAsync(Guid? index);

    }
}
