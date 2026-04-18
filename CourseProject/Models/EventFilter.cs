using Swashbuckle.AspNetCore.Annotations;

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
        public int Page { get; set; } = 1;

        [SwaggerSchema("Optional filter for end date and time (default = 10)", Format = "yyyy-MM-dd HH:mm:ss")]
        public int PageSize { get; set; } = 10;

    }
}
