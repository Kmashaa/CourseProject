using CourseProject.Presentation.Entities;
using CourseProject.Presentation.Models;
using System.Xml.Serialization;

namespace CourseProject.Presentation.Interfaces
{
    public interface IEventRepository
    {
        Task<List<Event>> GetAllAsync();

        Task<Event?> GetByIdAsync(Guid id);

        Task<Event> CreateAsync(Event @event);

        Task<Event> UpdateAsync(Event @event);

        Task<bool> DeleteAsync(Guid id);

        Task<PaginatedResult> GetEventsWithFilterAsync(EventFilter filter);

    }
}
