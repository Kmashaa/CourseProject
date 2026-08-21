using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CourseProject.Contracts
{
    public record EventSeatsUnavailable
    {
        public const string TopicName = "event-seats-unavailable";

        public Guid BookingId { get; init; }

        public Guid EventId { get; init; }

        public Guid UserId { get; init; }

        public string? Reason { get; init; }

        public DateTime CreatedAt { get; init; }

    }
}
