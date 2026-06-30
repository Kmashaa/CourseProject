using CourseProject.Entities;


namespace CourseProject.Exceptions
{
    public class EventNotFoundException : Exception
    {
        public Event? Event { get; }

        public EventNotFoundException() : base("Unknown event error")
        {

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
