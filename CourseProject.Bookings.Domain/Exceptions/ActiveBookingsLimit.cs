
using CourseProject.Bookings.Domain.Entities;


namespace CourseProject.Bookings.Domain.Exceptions
{
    public class ActiveBookingsLimit : Exception
    {
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
    }
}
