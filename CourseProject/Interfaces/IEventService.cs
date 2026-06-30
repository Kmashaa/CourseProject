using CourseProject.Entities;
using CourseProject.Models;

namespace CourseProject.Interfaces
{
    public interface IEventService
    {
        List<Event>? GetAllEvents();

        Event? GetEventById(Guid id);

        PaginatedResult GetEvents(EventFilter filter);

        Event CreateEvent(Event @event);
        
        Event UpdateEvent(Event @event);

        void DeleteEvent(Guid? index);

        PaginatedResult FilterEvents(List<Event> events, EventFilter filter);

    }
}
