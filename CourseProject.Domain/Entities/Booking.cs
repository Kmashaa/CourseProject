using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CourseProject.Domain.Entities
{
    public class Booking
    {
        public required Guid Id { get; init; }

        public required Guid EventId { get; init; }

        public required Guid UserId { get; init; }

        public required BookingStatus Status { get; set; }

        public required DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public Event? Event { get; set; }

        public User? User { get; set; }


        [SetsRequiredMembers]
        private Booking() { }

        [SetsRequiredMembers]
        public Booking(Guid id, Guid eventId, Guid userId, BookingStatus status, DateTime createdAt)
        {
            Id = id;
            EventId = eventId;
            UserId = userId;
            Status = status;
            CreatedAt = createdAt;
        }

        public void Confirm()
        {
            Status = BookingStatus.Confirmed;
            ProcessedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
        }

        public void Reject()
        {
            Status = BookingStatus.Rejected;
            ProcessedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
        }

        public void Cancel ()
        {
            if (Status != BookingStatus.Cancelled)
            {
                Status = BookingStatus.Cancelled;
                ProcessedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            }
            else
            {
                throw new Exception(); //TODO: exception
            }
        }

        public static Booking CreatePending(Guid eventId, Guid userId)
        {
            if (eventId == Guid.Empty)
                throw new ValidationException(nameof(EventId));

            if (userId == Guid.Empty)
                throw new ValidationException(nameof(UserId));


            return new Booking(Guid.NewGuid(), eventId, userId, BookingStatus.Pending, DateTime.UtcNow);
        }

    }

    public enum BookingStatus
    {
        Pending = 1,
        Confirmed = 2,
        Rejected = 3,
        Cancelled = 4
    }
}
