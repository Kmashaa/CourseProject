using CourseProject.Bookings.Application.Models;
using CourseProject.Bookings.Presentation.Models;


namespace CourseProject.Bookings.Presentation.Interfaces
{
    public interface IBookingModelDtoMapperService
    {
        BookingDto ModelToDto(BookingModel bookingDto);

        BookingModel DtoToModel(BookingDto booking);
    }
}
