using CourseProject.Entities;


namespace CourseProject.Exceptions
{
    public class InvalidEventDataException : Exception
    {
        public Event? Event { get; }

        public InvalidEventDataException() : base("Invalid event data")
        {

        }

        public InvalidEventDataException(Event @event, string message) : base(message)
        {
            Event = @event;
        }

        public InvalidEventDataException(Event @event, string message, Exception innerException) : base(message, innerException)
        {
            Event = @event;
        }
    }
}
