using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Events.Application.Interfaces
{
    public interface IEventSeatsReleasedProducer
    {
        Task PublishEventSeatsReleased(Guid bookingId, Guid eventId, Guid userId, CancellationToken ct = default);

    }
}
