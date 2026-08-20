using CourseProject.Domain.Entities;

namespace CourseProject.Domain.Exceptions
{
    public class InvalidUsersData : Exception
    {
        public InvalidUsersData() : base("Invalid user's data")
        {

        }
    }
}
