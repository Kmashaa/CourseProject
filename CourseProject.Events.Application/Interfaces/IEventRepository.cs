using CourseProject.Events.Domain.Entities;

namespace CourseProject.Events.Application.Interfaces
{
    public interface IEventRepository
    {
        Task<List<Event>> GetAllAsync();

        Task<Event?> GetByIdAsync(Guid id);

        Task<Event> CreateAsync(Event @event);

        Task<Event> UpdateAsync(Event @event);

        Task<bool> DeleteAsync(Guid id);

        Task<PaginatedResult> GetEventsWithFilterAsync(EventFilter filter);

        Task<List<TopEvent>> GetTopEventsAsync(int number);

    }
}
