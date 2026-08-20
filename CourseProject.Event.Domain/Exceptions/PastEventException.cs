using CourseProject.Events.Domain.Entities;

namespace CourseProject.Events.Domain.Exceptions
{
    public class PastEventException : Exception
    {
        public Event? Event { get; }

        public Guid? EventId { get; }

        public PastEventException() : base("Unknown event error")
        {

        }

        public PastEventException(Guid eventId) : base($"Event with ID '{eventId}' has already started")
        {
            EventId = eventId;
        }

        public PastEventException(Guid eventId, string message) : base(message)
        {
            EventId = eventId;
        }

        public PastEventException(Event @event, string message) : base(message)
        {
            Event = @event;
        }

        public PastEventException(Event @event, string message, Exception innerException) : base(message, innerException)
        {
            Event = @event;
        }
    }
}
