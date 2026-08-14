using CourseProject.Presentation.Entities;
using CourseProject.Presentation.Models;

namespace CourseProject.Presentation.Interfaces
{
    public interface IEventService
    {
        Task<List<Event>?> GetAllEventsAsync();

        Task<Event?> GetEventByIdAsync(Guid id);

        Task<PaginatedResult> GetEventsAsync(EventFilter filter);

        Task<Event> CreateEventAsync(Event @event);

        Task<Event> UpdateEventAsync(Event @event);

        Task<bool> DeleteEventAsync(Guid? index);

    }
}
