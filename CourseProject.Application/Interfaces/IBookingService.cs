using CourseProject.Application.Models;
using CourseProject.Domain.Entities;

namespace CourseProject.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingDto?> CreateBookingAsync(Guid? eventId, Guid userId);

        Task<BookingDto?> GetBookingByIdAsync(Guid bookingId);

        Task<bool> CancelBookingAsync(Guid bookingId, User user);
    }
}
