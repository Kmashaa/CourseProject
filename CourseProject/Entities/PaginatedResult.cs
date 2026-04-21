namespace CourseProject.Entities
{
    public class PaginatedResult
    {
        public int TotalItems { get; set; }

        public List<Event> Events { get; set; }

        public int CurrentPage { get; set; }

        public int NumOfItemsOnCurrentPage { get; set; }

    }
}
