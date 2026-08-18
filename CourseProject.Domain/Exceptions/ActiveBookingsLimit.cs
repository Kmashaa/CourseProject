using CourseProject.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Domain.Exceptions
{
    public class ActiveBookingsLimit : Exception
    {
        public User? User { get; }

        public Guid? UserId { get; }

        public ActiveBookingsLimit() : base("Unknown booking error")
        {

        }

        public ActiveBookingsLimit(Guid userId) : base($"User with ID '{userId}' has reached the limit of bookings")
        {
            UserId = userId;
        }

        public ActiveBookingsLimit(Guid userId, int limit) : base($"User with ID '{userId}' has reached the limit {limit} of bookings")
        {
            UserId = userId;
        }

        public ActiveBookingsLimit(Guid userId, string message) : base(message)
        {
            UserId = userId;
        }

        public ActiveBookingsLimit(User user, string message) : base(message)
        {
            User = user;
        }

        public ActiveBookingsLimit(User user, string message, Exception innerException) : base(message, innerException)
        {
            User = user;
        }
    }
}
