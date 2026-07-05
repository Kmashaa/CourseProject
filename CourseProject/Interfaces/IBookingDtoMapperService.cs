using CourseProject.Entities;
using CourseProject.Models;

namespace CourseProject.Interfaces
{
    public interface IBookingDtoMapperService
    {
        Booking DtoToEntity(BookingDto bookingDto);

        BookingDto EntityToDto(Booking booking);
    }
}
