namespace CourseProject.Entities
{
    public class Event
    {
        public required Guid Id { get; set; }

        public required string Title { get; set; }

        public string? Description { get; set; }

        public required DateTime StartAt { get; set; }

        public required DateTime EndAt { get; set; }

        public required int TotalSeats { get; set; }

        public int AvailableSeats { get; set; }

        public bool TryReserveSeats(int count = 1)
        {
            if (AvailableSeats >= 0 && AvailableSeats - count >= 0)
            {
                AvailableSeats -= count;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ReleaseSeats(int count)
        {
            AvailableSeats+= count;
            return true;
        }

    }
}
