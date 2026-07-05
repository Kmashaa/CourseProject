using CourseProject.Entities;
using CourseProject.Exceptions;
using CourseProject.Interfaces;

namespace CourseProject.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _repository;
        private readonly IEventService _eventService;

        public BookingService(IBookingRepository repository, IEventService eventService)
        {
            _repository = repository;
            _eventService = eventService;
        }


        public async Task<Booking?> CreateBookingAsync(Guid? eventId)
        {
            if (eventId == null)
            {
                throw new InvalidEventDataException();
            }

            var @event = _eventService.GetEventById((Guid)eventId);

            if (@event == null)
            {
                throw new EventNotFoundException();
            }

            return await _repository.CreateAsync((Guid)eventId);
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            return await _repository.GetByIdAsync(bookingId);
        }

    }
}
