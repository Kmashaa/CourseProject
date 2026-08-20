using CourseProject.Bookings.Application.Models;
using CourseProject.Bookings.Presentation.Interfaces;
using CourseProject.Bookings.Presentation.Models;

namespace CourseProject.Bookings.Presentation.Services
{
    public class BookingModelDtoMapperService : IBookingModelDtoMapperService
    {
        public BookingDto ModelToDto(BookingModel bookingModel)
        {
            BookingDto bookingDto = new(bookingModel.Id, bookingModel.EventId, bookingModel.UserId, (CourseProject.Bookings.Application.Models.BookingStatus)bookingModel.Status, bookingModel.CreatedAt)
            {
                Id = bookingModel.Id,
                EventId = bookingModel.EventId,
                UserId = bookingModel.UserId,
                Status = (Application.Models.BookingStatus)bookingModel.Status,
                CreatedAt = bookingModel.CreatedAt,
                ProcessedAt = bookingModel.ProcessedAt
            };


            return bookingDto;
        }

        public BookingModel DtoToModel(BookingDto bookingDto)
        {
            BookingModel bookingModel = new()
            {
                Id = bookingDto.Id,
                EventId = bookingDto.EventId,
                UserId = bookingDto.UserId,
                Status = (CourseProject.Bookings.Presentation.Models.BookingStatus)bookingDto.Status,
                CreatedAt = bookingDto.CreatedAt,
                ProcessedAt = bookingDto.ProcessedAt
            };
            return bookingModel;
        }

    }
}
