using CourseProject.Entities;
using CourseProject.Exceptions;
using CourseProject.Interfaces;

namespace CourseProject.Services
{
    public class BookingService : IBookingService
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly IBookingRepository _bookingRepository;
        private readonly IEventService _eventService;

        public BookingService(IBookingRepository repository, IEventService eventService)
        {
            _bookingRepository = repository;
            _eventService = eventService;
        }


        public async Task<Booking?> CreateBookingAsync(Guid? eventId)
        {
            if (eventId == null)
            {
                throw new InvalidEventDataException();
            }

            await _semaphore.WaitAsync();

            try
            {
                var @event = _eventService.GetEventById((Guid)eventId);

                if (@event == null)
                {
                    throw new EventNotFoundException();
                }

                if (@event.TryReserveSeats())
                {
                    _eventService.UpdateEvent(@event);
                    return await _bookingRepository.CreateAsync((Guid)eventId);
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
