using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Contracts
{
    public record EventSeatsReserved
    {
        public const string TopicName = "event-seats-reserved";

        public Guid BookingId { get; init; }

        public Guid EventId { get; init; }

        public DateTime CreatedAt { get; init; }

    }
}
