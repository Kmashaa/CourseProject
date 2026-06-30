using CourseProject.Entities;

namespace CourseProject.Interfaces
{
    public interface IBookingRepository
    {
        List<Booking> GetAll();

        Booking? GetById(Guid id);

        Booking Create(Booking booking);

        Booking? Update(Booking booking);

        bool Delete(Guid id);

    }
}
