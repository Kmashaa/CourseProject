using CourseProject.Application.Models;
using CourseProject.Domain.Entities;

namespace CourseProject.Application.Interfaces
{
    public interface IBookingDtoMapperService
    {
        Booking DtoToEntity(BookingDto bookingDto);

        BookingDto EntityToDto(Booking booking);
    }
}
