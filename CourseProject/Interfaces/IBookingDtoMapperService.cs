using CourseProject.Presentation.Entities;
using CourseProject.Presentation.Models;

namespace CourseProject.Presentation.Interfaces
{
    public interface IBookingDtoMapperService
    {
        Booking DtoToEntity(BookingDto bookingDto);

        BookingDto EntityToDto(Booking booking);
    }
}
