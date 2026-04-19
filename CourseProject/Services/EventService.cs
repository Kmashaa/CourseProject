using CourseProject.Entities;
using CourseProject.Interfaces;
using CourseProject.Models;

namespace CourseProject.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _repository;
        private readonly IEventDtoMapperService _eventDtoMapperService;


        public EventService(IEventRepository repository, IEventDtoMapperService eventDtoMapperService)
        {
            _repository = repository;
            _eventDtoMapperService = eventDtoMapperService;
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

            var paginated = filteredList.OrderBy(o => o.Id)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(o => _eventDtoMapperService.EntityToDto(o))
                    .ToList();


            PaginatedResult result = new PaginatedResult()
            {
                TotalItems = totalItems,
                CurrentPage = filter.Page,
                EventsDto = paginated,
                NumOfItemsOnCurrentPage = paginated.Count
            };

            return result;

        }

    }
}
