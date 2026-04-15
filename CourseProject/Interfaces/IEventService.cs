using CourseProject.Entities;

namespace CourseProject.Interfaces
{
    public interface IEventService
    {
        Event? GetEventById(int id);

        List<Event> GetAllEvents();

        void CreateEvent(Event @event);
        
        void UpdateEvent(Event @event);

        void DeleteEvent(int index);

    }
}
