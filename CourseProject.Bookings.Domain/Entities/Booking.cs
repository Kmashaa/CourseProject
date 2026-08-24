using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using CourseProject.Bookings.Domain.Exceptions;


namespace CourseProject.Bookings.Domain.Entities
{
    public class Booking
    {
        public required Guid Id { get; init; }

        public required Guid EventId { get; init; }

        public required Guid? UserId { get; init; }

        public required BookingStatus Status { get; set; }

        public required DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }


        [SetsRequiredMembers]
        private Booking() { }

        [SetsRequiredMembers]
        public Booking(Guid id, Guid eventId, Guid? userId, BookingStatus status, DateTime createdAt)
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

        public void Cancel()
        {
            if (Status == BookingStatus.Confirmed)
            {
                Status = BookingStatus.Cancelled;
                ProcessedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            }
            else if (Status == BookingStatus.Cancelled)
            {
                throw new BookingAlreadyInStatus(this);
            }
            else
            {
                throw new InvalidBookingDataException();
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
