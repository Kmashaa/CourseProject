using CourseProject.Presentation.DataAccess;
using CourseProject.Presentation.Entities;
using CourseProject.Presentation.Exceptions;
using CourseProject.Presentation.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseProject.Presentation.Services
{
    public class BookingService : IBookingService
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly IEventService _eventService;

        private readonly IBookingRepository _bookingRepository;

        public BookingService(IEventService eventService, IBookingRepository bookingRepository)
        {
            _eventService = eventService;
            _bookingRepository = bookingRepository;
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
                var @event = await _eventService.GetEventByIdAsync((Guid)eventId);

                if (@event == null)
                {
                    throw new EventNotFoundException();
                }

                if (@event.TryReserveSeats())
                {
                    var booking = Booking.CreatePending(@event.Id);
                    await _bookingRepository.CreateAsync(booking);
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
            return await _bookingRepository.GetByIdAsync(bookingId);
        }

    }
}
