using CourseProject.Application.Models;

namespace CourseProject.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingDto?> CreateBookingAsync(Guid? eventId);

        Task<BookingDto?> GetBookingByIdAsync(Guid bookingId);
    }
}
