using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace CourseProject.Models
{
    public class EventDto : IValidatableObject
    {
        [SwaggerSchema(ReadOnly = true)]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [SwaggerSchema("Title of event", Format = "string")]
        public string Title { get; set; }

        [SwaggerSchema("Description of event", Format = "string")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Start date and time are required")]
        [DataType(DataType.DateTime, ErrorMessage = "Wrong format of date and time")]
        [SwaggerSchema("Start date and time", Format = "yyyy-MM-dd HH:mm:ss")]
        public DateTime StartAt { get; set; }

        [Required(ErrorMessage = "End date and time are required")]
        [DataType(DataType.DateTime, ErrorMessage = "Wrong format of date and time")]
        [SwaggerSchema("End date and time", Format = "yyyy-MM-dd HH:mm:ss")]
        public DateTime EndAt { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndAt <= StartAt)
            {
                yield return new ValidationResult(
                    "The end date must be later than the start date"
                );
            }
        }
    }
}
