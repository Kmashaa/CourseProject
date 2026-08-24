using CourseProject.Bookings.Application.Models;
using CourseProject.Bookings.Domain.Entities;

namespace CourseProject.Bookings.Application.Interfaces
{
    public interface IBookingDtoMapperService
    {
        Booking DtoToEntity(BookingDto bookingDto);

        BookingDto EntityToDto(Booking booking);
    }
}
