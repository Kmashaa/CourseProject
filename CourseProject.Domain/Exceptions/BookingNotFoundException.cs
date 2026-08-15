using CourseProject.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Domain.Exceptions
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
