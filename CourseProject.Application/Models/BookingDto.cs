using System.Text.Json.Serialization;

namespace CourseProject.Application.Models
{
    public class BookingDto
    {

        public required Guid Id { get; set; }

        public required Guid EventId { get; set; }

        public required BookingStatus Status { get; set; }

        public required DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public BookingDto(Guid id, Guid eventId, BookingStatus status, DateTime createdAt, DateTime? processedAt = null)
        {
            Id = id;
            EventId = eventId;
            Status = status;
            CreatedAt = createdAt;
            ProcessedAt = processedAt;
        }

    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BookingStatus
    {
        Pending = 1,
        Confirmed = 2,
        Rejected = 3
    }

}
