using CourseProject.Domain.Entities;

namespace CourseProject.Domain.Exceptions
{
    public class EventNotFoundException : Exception
    {
        public Event? Event { get; }

        public Guid? EventId { get; }

        public EventNotFoundException() : base("Unknown event error")
        {

        }

        public EventNotFoundException(Guid eventId) : base($"Event with ID '{eventId}' was not found")
        {
            EventId = eventId;
        }

        public EventNotFoundException(Guid eventId, string message) : base(message)
        {
            EventId = eventId;
        }

        public EventNotFoundException(Event @event, string message) : base(message)
        {
            Event = @event;
        }

        public EventNotFoundException(Event @event, string message, Exception innerException) : base(message, innerException)
        {
            Event = @event;
        }
    }
}
