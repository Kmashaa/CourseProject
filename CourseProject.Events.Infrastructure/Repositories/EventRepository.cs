using CourseProject.Events.Application.Interfaces;
using CourseProject.Events.Domain.Entities;
using CourseProject.Events.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace CourseProject.Events.Infrastructure.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly AppDbContext _context;

        public EventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Event>> GetAllAsync()
        {
            return await _context.Events.ToListAsync();
        }

        public async Task<Event?> GetByIdAsync(Guid id)
        {
            var @event = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            return @event;
        }

        public async Task<Event> CreateAsync(Event @event)
        {
            await _context.Events.AddAsync(@event);
            await _context.SaveChangesAsync();
            return @event;
        }

        public async Task<Event> UpdateAsync(Event @event)
        {
            _context.Events.Update(@event);
            await _context.SaveChangesAsync();
            return @event;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var @event = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
                if (@event == null)
                {
                    return false;
                }
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        public async Task<PaginatedResult> GetEventsWithFilterAsync(EventFilter filter)
        {
            var query = _context.Events.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Title))
            {
                query = query.Where(e => e.Title != null && e.Title.ToLower().Contains(filter.Title.ToLower()));
            }

            if (filter.From.HasValue)
            {
                query = query.Where(e => e.StartAt >= filter.From.Value);
            }

            if (filter.To.HasValue)
            {
                query = query.Where(e => e.EndAt <= filter.To.Value);
            }

            int totalItems = await query.CountAsync();

            var paginatedEvents = await query
                .OrderBy(o => o.StartAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResult
            {
                TotalItems = totalItems,
                CurrentPage = filter.Page,
                Events = paginatedEvents,
                NumOfItemsOnCurrentPage = paginatedEvents.Count
            };
        }

        public async Task<List<TopEvent>> GetTopEventsAsync(int number)
        {
            var topEvents = await _context.Events
                .Where(e => e.TotalSeats > 0) 
                .Select(e => new TopEvent
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    TotalSeats = e.TotalSeats,
                    AvailableSeats = e.AvailableSeats,
                    StartAt = e.StartAt,
                    EndAt = e.EndAt,
                    SalesPercentage = Math.Round((decimal)(e.TotalSeats - e.AvailableSeats) / e.TotalSeats * 100, 2)
                })
                .OrderByDescending(e => e.SalesPercentage) 
                .ThenByDescending(e => e.TotalSeats) 
                .Take(number)
                .ToListAsync();

            return topEvents;
        }

    }
}
