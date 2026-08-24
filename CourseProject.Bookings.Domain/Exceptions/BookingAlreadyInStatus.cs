using CourseProject.Bookings.Domain.Entities;

namespace CourseProject.Bookings.Domain.Exceptions
{
    public class BookingAlreadyInStatus : Exception
    {
        public Booking? Booking { get; }


        public BookingAlreadyInStatus() : base("Booking is already in this status")
        {

        }

        public BookingAlreadyInStatus(Booking booking) : base($"Booking with ID '{booking.Id}' is already in status {booking.Status.ToString()}")
        {
            Booking = booking;
        }



        public BookingAlreadyInStatus(Booking booking, string message) : base(message)
        {
            Booking = booking;
        }


        public BookingAlreadyInStatus(Booking booking, string message, Exception innerException) : base(message, innerException)
        {
            Booking = booking;
        }
    }
}
