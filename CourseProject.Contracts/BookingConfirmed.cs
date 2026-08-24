using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Contracts
{
    public record BookingConfirmed
    {
        public const string TopicName = "booking-confirmed";

        public Guid BookingId { get; init; }

        public Guid EventId { get; init; }

        public Guid UserId { get; init; }

        public int NumOfSeats { get; init; }

        public DateTime CreatedAt { get; init; }
    }
}
