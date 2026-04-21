using CourseProject.Entities;
using CourseProject.Exceptions;
using CourseProject.Interfaces;
using CourseProject.Models;

namespace CourseProject.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _repository;

        public EventService(IEventRepository repository, IEventDtoMapperService eventDtoMapperService)
        {
            _repository = repository;
        }
        
        public List<Event>? GetAllEvents()
        {
            var events = _repository.GetAll();
            return events;
        }

        public PaginatedResult GetEvents(EventFilter filter)
        {
            var events = GetAllEvents();
            var filteredEvents = FilterEvents(events, filter);
            return filteredEvents;
        }

        public Event? GetEventById(Guid id)
        {
            return _repository.GetById(id);
        }

        public Event CreateEvent(Event @event)
        {
            ValidateEvent(@event);
            return _repository.Create(@event);
        }

        public Event UpdateEvent(Event @event)
        {
            ValidateEvent(@event);
            return _repository.Update(@event);
        }

        public Guid DeleteEvent(Guid? id)
        {
            if (id == null)
            {
                throw new InvalidEventDataException();
            }
            return _repository.Delete((Guid)id);
        }

        public PaginatedResult FilterEvents(List<Event> events, EventFilter filter)
        {
            var filtered = events.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.Title))
            {
                filtered = filtered.Where(e => e.Title != null &&
                                               e.Title.Contains(filter.Title, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.From.HasValue)
            {
                filtered = filtered.Where(e => e.StartAt >= filter.From.Value);
            }

            if (filter.To.HasValue)
            {
                filtered = filtered.Where(e => e.EndAt <= filter.To.Value);
            }

            var filteredList = filtered.ToList();
            int totalItems = filteredList.Count;

            var paginated = filteredList.OrderBy(o => o.StartAt)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToList();


            PaginatedResult result = new PaginatedResult()
            {
                TotalItems = totalItems,
                CurrentPage = filter.Page,
                Events = paginated,
                NumOfItemsOnCurrentPage = paginated.Count
            };

            return result;

        }

        private void ValidateEvent(Event? @event)
        {
            if (@event == null)
            {
                throw new InvalidEventDataException();
            }
            if (@event.StartAt >= @event.EndAt)
            {
                throw new InvalidEventDataException();
            }
            if (String.IsNullOrWhiteSpace(@event.Title))
            {
                throw new InvalidEventDataException();
            }
        }

    }
}
