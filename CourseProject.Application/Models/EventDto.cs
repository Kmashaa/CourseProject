using System.ComponentModel.DataAnnotations;

namespace CourseProject.Application.Models
{
    public class EventDto : IValidatableObject
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string? Description { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public int? TotalSeats { get; set; }

        public int? AvailableSeats { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndAt <= StartAt)
            {
                yield return new ValidationResult(
                    "The end date must be later than the start date"
                );
            }
        }

        public EventDto(
        Guid id,
        string title,
        DateTime startAt,
        DateTime endAt,
        int totalSeats,
        int availableSeats,
        string? description = null)
        {
            Id = id;
            Title = title;
            StartAt = startAt;
            EndAt = endAt;
            TotalSeats = totalSeats;
            AvailableSeats = availableSeats;
            Description = description;
        }

        public EventDto(
        Guid id,
        string title,
        DateTime startAt,
        DateTime endAt,
        int totalSeats,
        string? description = null)
        {
            Id = id;
            Title = title;
            StartAt = startAt;
            EndAt = endAt;
            TotalSeats = totalSeats;
            AvailableSeats = totalSeats;
            Description = description;
        }
    }
}
