namespace CourseProject.Entities
{
    public class Booking
    {
        public required Guid Id { get; set; }

        public required Guid EventId { get; set; }

        public required BookingStatus Status { get; set; }

        public required DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

    }

    public enum BookingStatus
    {
        Pending = 1,
        Confirmed = 2,
        Rejected = 3
    }
}
