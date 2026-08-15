using CourseProject.Presentation.Models;
using CourseProject.Application.Models;


namespace CourseProject.Presentation.Interfaces
{
    public interface IBookingModelDtoMapperService
    {
        BookingDto ModelToDto(BookingModel bookingDto);

        BookingModel DtoToModel(BookingDto booking);
    }
}
