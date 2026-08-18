using System.Text.Json.Serialization;

namespace CourseProject.Application.Models
{
    public class BookingDto
    {

        public required Guid Id { get; set; }

        public required Guid EventId { get; set; }

        public required Guid? UserId { get; set; }

        public required BookingStatus Status { get; set; }

        public required DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public BookingDto(Guid id, Guid eventId, Guid? userId, BookingStatus status, DateTime createdAt, DateTime? processedAt = null)
        {
            Id = id;
            EventId = eventId;
            UserId = userId;
            Status = status;
            CreatedAt = createdAt;
            ProcessedAt = processedAt;
        }

    }

    public enum BookingStatus
    {
        Pending = 1,
        Confirmed = 2,
        Rejected = 3
    }

}
