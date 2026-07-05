using System.Net.NetworkInformation;

namespace CourseProject.Entities
{
    public class Booking
    {
        public required Guid Id { get; init; }

        public required Guid EventId { get; set; }

        public required BookingStatus Status { get; set; }

        public required DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public void Confirm()
        {
            Status = BookingStatus.Confirmed;
            ProcessedAt = DateTime.Now;
        }

        public void Reject()
        {
            Status = BookingStatus.Rejected;
            ProcessedAt = DateTime.Now;
        }

    }

    public enum BookingStatus
    {
        Pending = 1,
        Confirmed = 2,
        Rejected = 3
    }
}
