namespace CourseProject.Events.Presentation.Models
{
    public class PaginatedResultModel
    {
        public int TotalItems { get; set; }

        public List<EventModel> EventsModel { get; set; }

        public int CurrentPage { get; set; }

        public int NumOfItemsOnCurrentPage { get; set; }

    }
}
