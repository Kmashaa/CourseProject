using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CourseProject.Events.Application.Interfaces
{
    public interface IEventSeatsUnavailableProducer
    {
        Task PublishEventSeatsUnavailable(Guid bookingId, Guid eventId, Guid userId, string? reason, CancellationToken ct = default);

    }
}
