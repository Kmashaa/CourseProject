using CourseProject.Domain.Entities;


namespace CourseProject.Domain.Exceptions
{
    public class NoAvailableSeatsException : Exception
    {
        public Event? Event { get; }

        public NoAvailableSeatsException() : base("No available seats for this event")
        {

        }

        public NoAvailableSeatsException(Event @event, string message) : base(message)
        {
            Event = @event;
        }

        public NoAvailableSeatsException(Event @event, string message, Exception innerException) : base(message, innerException)
        {
            Event = @event;
        }

    }
}
