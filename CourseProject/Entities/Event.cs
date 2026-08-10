using System.Diagnostics.CodeAnalysis;

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

        public ICollection<Booking> Bookings { get; set; } = [];


        [SetsRequiredMembers]
        private Event()
        {
            Title = null!;
        }

        [SetsRequiredMembers]
        public Event(
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

        public static Event Create(
            string? title,
            DateTime? startAt,
            DateTime? endAt,
            int? totalSeats = null,
            string? description = null)
        {
            return new Event(Guid.NewGuid(), title!.Trim(), startAt!.Value, endAt!.Value, totalSeats!.Value, description);
        }

        internal void Update(
           string? title,
           DateTime? startAt,
           DateTime? endAt,
           string? description = null)
        {
            Title = title!;
            StartAt = startAt!.Value;
            EndAt = endAt!.Value;
            Description = description;
        }

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

        public bool ReleaseSeats(int count = 1)
        {
            AvailableSeats += count;
            return true;
        }

    }
}
