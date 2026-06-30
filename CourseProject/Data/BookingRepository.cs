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

        public List<Booking> GetAll()
        {
            return _bookings;
        }

        public Booking? GetById(Guid id)
        {
            return _bookings.FirstOrDefault(o => o.Id == id);
        }

        public Booking Create(Booking booking)
        {
            booking.Id = Guid.NewGuid();
            booking.Status = BookingStatus.Pending;
            booking.CreatedAt = DateTime.Now;

            _bookings.Add(booking);
            return booking;
        }

        public Booking? Update(Booking booking)
        {
            var index = _bookings.FindIndex(o => o.Id == booking.Id);
            if (index != -1)
            {
                _bookings[index] = booking;
                return _bookings[index];
            }

            return null;

        }

        public bool Delete(Guid id)
        {
            var eventFromList = _bookings.FirstOrDefault(o => o.Id == id);

            if (eventFromList != null)
            {
                _bookings.Remove(eventFromList);
                return true;
            }

            return false;
        }
    }
}
