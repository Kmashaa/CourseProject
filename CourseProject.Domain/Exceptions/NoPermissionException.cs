using CourseProject.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Domain.Exceptions
{
    public class NoPermissionException : Exception
    {
        public User? User { get; }

        public Guid? UserId { get; }

        public NoPermissionException() : base("Unknown user error")
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

        public NoPermissionException(User user, string message) : base(message)
        {
            User = user;
        }

        public NoPermissionException(User user, string message, Exception innerException) : base(message, innerException)
        {
            User = user;
        }
    }
}
