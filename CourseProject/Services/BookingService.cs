using CourseProject.Entities;
using CourseProject.Exceptions;
using CourseProject.Interfaces;

namespace CourseProject.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _repository;

        public BookingService(IBookingRepository repository)
        {
            _repository = repository;
        }


        public async Task<Booking?> CreateBookingAsync(Guid? eventId)
        {
            if (eventId == null)
            {
                throw new InvalidEventDataException();
            }

            return await _repository.CreateAsync((Guid)eventId);
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            return await _repository.GetByIdAsync(bookingId);
        }

    }
}
