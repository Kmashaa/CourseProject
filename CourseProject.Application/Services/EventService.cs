using CourseProject.Application.Exceptions;
using CourseProject.Application.Interfaces;
using CourseProject.Application.Models;
using CourseProject.Domain.Exceptions;

namespace CourseProject.Application.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IEventDtoMapperService _eventDtoMapperService;
        private readonly IEventFilterDtoMapperService _eventFilterDtoMapperService;
        private readonly IPaginatedResultDtoMapperService _paginatedResultDtoMapperService;


        public EventService(IEventRepository eventRepository, IEventDtoMapperService eventDtoMapperService, IEventFilterDtoMapperService eventFilterDtoMapperService, IPaginatedResultDtoMapperService paginatedResultDtoMapperService)
        {
            _eventRepository = eventRepository;
            _eventDtoMapperService = eventDtoMapperService;
            _eventFilterDtoMapperService = eventFilterDtoMapperService;
            _paginatedResultDtoMapperService = paginatedResultDtoMapperService;
        }

        public async Task<List<EventDto>?> GetAllEventsAsync()
        {
            var events = await _eventRepository.GetAllAsync();
            return events.Select(o => _eventDtoMapperService.EntityToDto(o)).ToList();
        }

        public async Task<PaginatedResultDto> GetEventsAsync(EventFilterDto filter)
        {
            var filteredEvents = await _eventRepository.GetEventsWithFilterAsync(_eventFilterDtoMapperService.DtoToEntity(filter));
            return _paginatedResultDtoMapperService.EntityToDto(filteredEvents);
        }

        public async Task<EventDto?> GetEventByIdAsync(Guid id)
        {
            var @event = await _eventRepository.GetByIdAsync(id);

            if (@event == null)
            {
                throw new EventNotFoundException(@event, "Event not found");

            }

            return _eventDtoMapperService.EntityToDto(@event);
        }

        public async Task<EventDto> CreateEventAsync(EventDto @event)
        {
            ValidateEvent(@event);
            @event.AvailableSeats = @event.TotalSeats;
            @event.Id = Guid.NewGuid();
            await _eventRepository.CreateAsync(_eventDtoMapperService.DtoToEntity(@event));
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

            return @event;
        }

        public async Task<bool> DeleteEventAsync(Guid? id)
        {
            if (id == null)
            {
                throw new InvalidEventDataException();
            }
            return await _eventRepository.DeleteAsync((Guid)id);
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
