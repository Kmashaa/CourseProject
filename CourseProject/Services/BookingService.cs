using CourseProject.DataAccess;
using CourseProject.Entities;
using CourseProject.Exceptions;
using CourseProject.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseProject.Services
{
    public class BookingService : IBookingService
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly AppDbContext _context;

        public BookingService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<Booking?> CreateBookingAsync(Guid? eventId)
        {
            if (eventId == null)
            {
                throw new InvalidEventDataException();
            }

            await _semaphore.WaitAsync(); //semaphore, а не lock, т.к. внутри асинхронный код

            try
            {
                var @event = await _context.Events.FirstOrDefaultAsync(o => o.Id == eventId);

                if (@event == null)
                {
                    throw new EventNotFoundException();
                }

                if (@event.TryReserveSeats())
                {
                    var booking = Booking.CreatePending(@event.Id);
                    await _context.Bookings.AddAsync(booking);
                    await _context.SaveChangesAsync();
                    return booking;
                }
                else
                {
                    throw new NoAvailableSeatsException();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            return await _context.Bookings.FirstOrDefaultAsync(o=>o.Id==bookingId);
        }

    }
}
