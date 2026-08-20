using CourseProject.Bookings.Domain.Entities;

namespace CourseProject.Bookings.Domain.Exceptions
{
    public class BookingNotFoundException: Exception
    {
        public Booking? Booking { get; }

        public BookingNotFoundException() : base("Unknown booking error")
        {

        }

        public BookingNotFoundException(Booking booking, string message) : base(message)
        {
            Booking = booking;
        }

        public BookingNotFoundException(Booking booking, string message, Exception innerException) : base(message, innerException)
        {
            Booking = booking;
        }
    }
}
