using CourseProject.Events.Application.Exceptions;
using CourseProject.Events.Application.Interfaces;
using CourseProject.Events.Application.Models;
using CourseProject.Events.Domain.Exceptions;

namespace CourseProject.Events.Application.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IEventDtoMapperService _eventDtoMapperService;
        private readonly IEventFilterDtoMapperService _eventFilterDtoMapperService;
        private readonly IPaginatedResultDtoMapperService _paginatedResultDtoMapperService;
        private readonly ITopEventDtoMapperService _topEventDtoMapperService;
        private readonly ICacheService _cache;


        public EventService(IEventRepository eventRepository, IEventDtoMapperService eventDtoMapperService, IEventFilterDtoMapperService eventFilterDtoMapperService, IPaginatedResultDtoMapperService paginatedResultDtoMapperService, ITopEventDtoMapperService topEventDtoMapperService, ICacheService cache)
        {
            _eventRepository = eventRepository;
            _eventDtoMapperService = eventDtoMapperService;
            _eventFilterDtoMapperService = eventFilterDtoMapperService;
            _paginatedResultDtoMapperService = paginatedResultDtoMapperService;
            _topEventDtoMapperService = topEventDtoMapperService;
            _cache = cache;
        }

        public async Task<PaginatedResultDto> GetEventsAsync(EventFilterDto filter)
        {
            var filteredEvents = await _eventRepository.GetEventsWithFilterAsync(_eventFilterDtoMapperService.DtoToEntity(filter));
            return _paginatedResultDtoMapperService.EntityToDto(filteredEvents);
        }

        public async Task<EventDto?> GetEventByIdAsync(Guid id)
        {
            var cachedEvent = await _cache.GetById(id);
            if (cachedEvent != null)
            {
                return _eventDtoMapperService.EntityToDto(cachedEvent);
            }

            var @event = await _eventRepository.GetByIdAsync(id);

            if (@event == null)
            {
                throw new EventNotFoundException(id);

            }

            await _cache.SetById(id, @event);

            return _eventDtoMapperService.EntityToDto(@event);
        }

        public async Task<List<TopEventDto>> GetTopEvents(int number)
        {
            var cachedEvent = await _cache.GetTop(number);
            if (cachedEvent != null)
            {
                return cachedEvent.Select(o => _topEventDtoMapperService.EntityToDto(o)).ToList();
            }

            var events = await _eventRepository.GetTopEventsAsync(number);

            if (events == null)
            {
                throw new EventNotFoundException();

            }

            await _cache.SetTop(number, events);

            return events.Select(o => _topEventDtoMapperService.EntityToDto(o)).ToList();
        }

        public async Task<EventDto> CreateEventAsync(EventDto @event)
        {
            ValidateEvent(@event);
            @event.AvailableSeats = @event.TotalSeats;
            @event.Id = Guid.NewGuid();

            var entityEvent = _eventDtoMapperService.DtoToEntity(@event);
            await _eventRepository.CreateAsync(entityEvent);
            await _cache.SetById(@event.Id, entityEvent);

            return @event;
        }

        public async Task<EventDto> UpdateEventAsync(EventDto @event)
        {
            ValidateEvent(@event);

            var currentDbEvent = await _eventRepository.GetByIdAsync(@event.Id);

            if (currentDbEvent == null)
            {
                throw new EventNotFoundException(currentDbEvent, "Event not found");
            }

            int bookedSeats = currentDbEvent.TotalSeats - currentDbEvent.AvailableSeats;

            if (@event.TotalSeats - bookedSeats < 0)
            {
                throw new InvalidEventDataException();

            }

            @event.AvailableSeats = @event.TotalSeats - bookedSeats;

            currentDbEvent.Title = @event.Title;
            currentDbEvent.StartAt = @event.StartAt;
            currentDbEvent.EndAt = @event.EndAt;
            currentDbEvent.TotalSeats = (int)@event.TotalSeats;
            currentDbEvent.AvailableSeats = (int)@event.AvailableSeats;
            currentDbEvent.Description = @event.Description;

            await _eventRepository.UpdateAsync(currentDbEvent);

            await _cache.SetById(currentDbEvent.Id, currentDbEvent);

            return @event;
        }

        public async Task<bool> DeleteEventAsync(Guid? id)
        {
            if (id == null)
            {
                throw new InvalidEventDataException();
            }

            var deleted = await _eventRepository.DeleteAsync((Guid)id);
            await _cache.DeleteById((Guid)id);

            return deleted;
        }


        private void ValidateEvent(EventDto? @event)
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
            if (@event.TotalSeats <= 0)
            {
                throw new InvalidEventDataException();
            }

        }

    }
}
