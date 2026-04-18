using CourseProject.Entities;
using CourseProject.Models;

namespace CourseProject.Interfaces
{
    public interface IEventService
    {
        Event? GetEventById(int id);

        List<Event> GetEvents(EventFilter filter);

        void CreateEvent(Event @event);
        
        void UpdateEvent(Event @event);

        void DeleteEvent(int index);

    }
}
