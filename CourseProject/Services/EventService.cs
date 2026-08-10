using CourseProject.DataAccess;
using CourseProject.Entities;
using CourseProject.Exceptions;
using CourseProject.Interfaces;
using CourseProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseProject.Services
{
    public class EventService : IEventService
    {
        private readonly AppDbContext _context;

        public EventService(AppDbContext context, IEventDtoMapperService eventDtoMapperService)
        {
            _context = context;
        }

        public async Task<List<Event>?> GetAllEventsAsync()
        {
            var events = await _context.Events.ToListAsync();
            return events;
        }

        public async Task<PaginatedResult> GetEventsAsync(EventFilter filter)
        {
            var events = await GetAllEventsAsync();
            var filteredEvents = FilterEvents(events, filter);
            return filteredEvents;
        }

        public async Task<Event?> GetEventByIdAsync(Guid id)
        {
            return await _context.Events.FirstOrDefaultAsync(o=>o.Id==id);
        }

        public async Task<Event> CreateEventAsync(Event @event)
        {
            ValidateEvent(@event);
            @event.AvailableSeats = @event.TotalSeats;
            await _context.Events.AddAsync(@event);
            await _context.SaveChangesAsync();
            return @event;
        }

        public async Task<Event> UpdateEventAsync(Event @event)
        {
            ValidateEvent(@event);

            var eventToUpdate = await GetEventByIdAsync(@event.Id);
            if (eventToUpdate == null)
            {
                throw new EventNotFoundException();
            }

            eventToUpdate.Update(@event.Title, @event.StartAt, @event.EndAt, @event.Description);

            await _context.SaveChangesAsync();


            return eventToUpdate;
        }

        public async Task DeleteEventAsync(Guid? id)
        {
            if (id == null)
            {
                throw new InvalidEventDataException();
            }
            var @event = await GetEventByIdAsync((Guid)id);
            if (@event == null) { return; }
            _context.Events.Remove(@event);
            await _context.SaveChangesAsync();
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
            if (@event.TotalSeats <= 0)
            {
                throw new InvalidEventDataException();
            }
        }

    }
}
