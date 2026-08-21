using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Bookings.Application.Interfaces
{
    public interface IBookingCancelledProducer
    {
        Task PublishBookingCancelled(Guid bookingId, Guid eventId, Guid userId, int numOfSeats = 1, CancellationToken ct = default);

    }
}
