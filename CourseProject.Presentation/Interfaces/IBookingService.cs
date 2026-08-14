using CourseProject.Presentation.Entities;

namespace CourseProject.Presentation.Interfaces
{
    public interface IBookingService
    {
        Task<Booking?> CreateBookingAsync(Guid? eventId);

        Task<Booking?> GetBookingByIdAsync(Guid bookingId);
    }
}
