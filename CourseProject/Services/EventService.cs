using CourseProject.Entities;
using CourseProject.Interfaces;
using CourseProject.Models;

namespace CourseProject.Services
{
    public class EventService: IEventService
    {
        private readonly IEventRepository _repository;

        public EventService(IEventRepository repository)
        {
            _repository = repository;
        }

        public List<Event> GetEvents(EventFilter filter)
        {
            var events = _repository.GetAll();
            var filteredEvents= FilterEvents(events, filter);
            return filteredEvents;
        }

        public Event? GetEventById(int id)
        {
            return _repository.GetById(id);
        }

        public void CreateEvent(Event @event)
        {
            _repository.Create(@event);
        }

        public void UpdateEvent(Event @event)
        {
            _repository.Update(@event);
        }

        public void DeleteEvent(int id)
        {
            _repository.Delete(id);
        }

        public List<Event> FilterEvents(List<Event> events, EventFilter filter)
        {
            var filtered = events.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.Title))
            {
                filtered = filtered.Where(e => e.Title != null && 
                                               e.Title.Contains(filter.Title, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.From.HasValue)
            {
                filtered = filtered.Where(e => e.StartAt>=filter.From.Value);
            }

            if (filter.To.HasValue)
            {
                filtered = filtered.Where(e => e.EndAt <= filter.To.Value);
            }

            return filtered.ToList();

        }

    }
}
