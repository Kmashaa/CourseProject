using CourseProject.Bookings.Domain.Entities;

namespace CourseProject.Bookings.Application.Interfaces
{
    public interface IBookingRepository
    {
        Task<List<Booking>> GetAllAsync();

        Task<Booking?> GetByIdAsync(Guid id);

        Task<Booking> CreateAsync(Booking booking);

        Task<Booking?> UpdateAsync(Booking booking);

        Task<bool> DeleteAsync(Guid id);

        Task<List<Guid>> GetPendingsAsync();

        Task<int> GetActiveBookingsCountByUserIdAsync(Guid userId);

    }
}
