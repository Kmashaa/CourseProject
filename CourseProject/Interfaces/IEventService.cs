using CourseProject.Entities;

namespace CourseProject.Interfaces
{
    public interface IEventService
    {
        Event? GetEventById(int id);

        List<Event> GetAllEvents();

        void CreateEvent(Event newEvent); //TODO: через string
        
        void UpdateEvent(Event updatedEvent); //TODO: через string

        void DeleteEvent(int index);

    }
}
