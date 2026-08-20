using CourseProject.Bookings.Domain.Entities;

namespace CourseProject.Bookings.Domain.Exceptions
{
    public class NoPermissionException : Exception
    {
        public Guid? UserId { get; }

        public NoPermissionException() : base("No permission")
        {

        }

        public NoPermissionException(Guid userId) : base($"User with ID '{userId}' has no rights for the operation")
        {
            UserId = userId;
        }

        public NoPermissionException(Guid userId, string message) : base(message)
        {
            UserId = userId;
        }
    }
}
