using CourseProject.Entities;

namespace CourseProject.Interfaces
{
    public interface IBookingRepository
    {
        Task<List<Booking>> GetAllAsync();

        Task<Booking?> GetByIdAsync(Guid id);

        Task<Booking> CreateAsync(Guid eventId);

        Task<Booking?> UpdateAsync(Booking booking);

        Task<bool> DeleteAsync(Guid id);

        Task<List<Booking>> GetPendingsAsync();

    }
}
