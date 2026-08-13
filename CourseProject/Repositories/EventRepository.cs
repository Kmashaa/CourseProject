using CourseProject.DataAccess;
using CourseProject.Entities;
using CourseProject.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseProject.Repositories
{
    public class EventRepository: IEventRepository
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

        public async Task<Event?> UpdateAsync(Event @event)
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
            catch (Exception)
            {
                return false;
            }
        }
        //TODO: filter
    }
}
