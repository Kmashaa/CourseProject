using CourseProject.Entities;
using System.Xml.Serialization;

namespace CourseProject.Interfaces
{
    public interface IEventRepository
    {
        Task<List<Event>> GetAllAsync();

        Task<Event?> GetByIdAsync(Guid id);

        Task<Event> CreateAsync(Event @event);

        Task<Event?> UpdateAsync(Event @event);

        Task<bool> DeleteAsync(Guid id);
    }
}
