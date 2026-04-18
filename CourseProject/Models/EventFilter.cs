using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace CourseProject.Models
{
    public class EventFilter
    {
        [SwaggerSchema("Optional filter for title of event (partial match, case insensitive)", Format = "string")]
        public string? Title { get; set; }

        [SwaggerSchema("Optional filter for start date and time (events don't start before this date)", Format = "yyyy-MM-dd HH:mm:ss")]
        public DateTime? From { get; set; }

        [SwaggerSchema("Optional filter for end date and time (events end no later than this date)", Format = "yyyy-MM-dd HH:mm:ss")]
        public DateTime? To { get; set; }

        [SwaggerSchema("Optional filter for number of elements per page (default = 1)", Format = "int")]
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int Page { get; set; } = 1;

        [SwaggerSchema("Optional filter for end date and time (default = 10)", Format = "yyyy-MM-dd HH:mm:ss")]
        [Range(1, int.MaxValue, ErrorMessage = "Page size must be greater than 0")]
        public int PageSize { get; set; } = 10;

    }
}
