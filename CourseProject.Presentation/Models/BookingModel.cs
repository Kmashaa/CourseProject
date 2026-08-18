using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace CourseProject.Presentation.Models
{
    public class BookingModel
    {

        [SwaggerSchema("Id of booking", ReadOnly = true)]
        public required Guid Id { get; set; }

        [SwaggerSchema("Id of event", ReadOnly = true)]
        public required Guid EventId { get; set; }

        [SwaggerSchema("Id of user", ReadOnly = true)]
        public required Guid? UserId { get; set; }

        [SwaggerSchema("Status of booking", ReadOnly = true)]
        public required BookingStatus Status { get; set; }

        [SwaggerSchema("Date and time when booking was created", ReadOnly = true)]
        public required DateTime CreatedAt { get; set; }

        [SwaggerSchema("Date and time when booking was processed", ReadOnly = true)]
        public DateTime? ProcessedAt { get; set; }

    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BookingStatus
    {
        Pending = 1,
        Confirmed = 2,
        Rejected = 3
    }

}
