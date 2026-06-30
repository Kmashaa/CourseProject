using CourseProject.Entities;
using CourseProject.Interfaces;

namespace CourseProject.Data
{
    public class BookingRepository : IBookingRepository
    {
        private readonly List<Booking> _bookings = new();

        public BookingRepository()
        {
        }

        public Task<List<Booking>> GetAllAsync()
        {
            return Task.FromResult(_bookings);
        }

        public Task<Booking?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_bookings.FirstOrDefault(o => o.Id == id));
        }

        public Task<Booking> CreateAsync(Guid eventId)
        {
            Booking booking = new()
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _bookings.Add(booking);
            return Task.FromResult(booking);
        }

        public Task<Booking?> UpdateAsync(Booking booking)
        {
            var index = _bookings.FindIndex(o => o.Id == booking.Id);
            if (index != -1)
            {
                _bookings[index] = booking;
                return Task.FromResult(_bookings[index]);
            }

            return Task.FromResult<Booking?>(null);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            var eventFromList = _bookings.FirstOrDefault(o => o.Id == id);

            if (eventFromList != null)
            {
                _bookings.Remove(eventFromList);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
