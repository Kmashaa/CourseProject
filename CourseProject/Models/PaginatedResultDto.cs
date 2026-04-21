namespace CourseProject.Models
{
    public class PaginatedResultDto
    {
        public int TotalItems { get; set; }

        public List<EventDto> EventsDto { get; set; }

        public int CurrentPage { get; set; }

        public int NumOfItemsOnCurrentPage { get; set; }

    }
}
