using CourseProject.Bookings.Domain.Entities;


namespace CourseProject.Bookings.Domain.Exceptions
{
    public class InvalidBookingDataException : Exception
    {
        public Booking? Booking { get; }

        public InvalidBookingDataException() : base("Invalid booking data")
        {

        }

        public InvalidBookingDataException(Booking booking, string message) : base(message)
        {
            Booking = booking;
        }

        public InvalidBookingDataException(Booking booking, string message, Exception innerException) : base(message, innerException)
        {
            Booking = booking;
        }
    }
}
