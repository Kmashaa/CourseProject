using CourseProject.Bookings.Application.Models;
using CourseProject.Bookings.Domain.Entities;

namespace CourseProject.Bookings.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingDto?> CreateBookingAsync(Guid? eventId, Guid userId);

        Task<BookingDto?> GetBookingByIdAsync(Guid bookingId, Guid userId, string role);

        Task<BookingDto?> CancelBookingAsync(Guid bookingId, Guid userId, string role);
    }
}
