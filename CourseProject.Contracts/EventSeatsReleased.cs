using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Contracts
{
    public record EventSeatsReleased
    {
        public const string TopicName = "event-seats-released";

        public Guid BookingId { get; init; }

        public Guid EventId { get; init; }

        public Guid UserId { get; init; }

        public DateTime CreatedAt { get; init; }

    }
}
