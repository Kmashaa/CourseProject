using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
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

        public Event? Event { get; set; }

        [SetsRequiredMembers]
        private Booking() { }

        [SetsRequiredMembers]
        private Booking(Guid id, Guid eventId, BookingStatus status, DateTime createdAt)
        {
            Id = id;
            EventId = eventId;
            Status = status;
            CreatedAt = createdAt;
        }

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

        public static Booking CreatePending(Guid eventId)
        {
            if (eventId == Guid.Empty)
                throw new ValidationException(nameof(EventId));

            return new Booking(Guid.NewGuid(), eventId, BookingStatus.Pending, DateTime.UtcNow);
        }

    }

    public enum BookingStatus
    {
        Pending = 1,
        Confirmed = 2,
        Rejected = 3
    }
}
