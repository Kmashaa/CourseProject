using CourseProject.DataAccess;
using CourseProject.Entities;
using CourseProject.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseProject.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Booking>> GetAllAsync()
        {
            return await _context.Bookings.ToListAsync();

        }

        public async Task<Booking?> GetByIdAsync(Guid id)
        {
            return await _context.Bookings.FirstOrDefaultAsync(o => o.Id == id);

        }

        public async Task<Booking> CreateAsync(Booking booking)
        {
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();
            return booking;

        }

        public async Task<Booking?> UpdateAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            return booking;

        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var booking = await _context.Bookings.FirstOrDefaultAsync(e => e.Id == id);
                if (booking == null)
                {
                    return false;
                }
                _context.Booki.Remove(booking);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Guid>> GetPendingsAsync()
        {
            var pendings = _context.Bookings
                .Where(b => b.Status == BookingStatus.Pending)
                .Select(o => o.Id)
                .ToList();

            return pendings;

        }
    }
}
