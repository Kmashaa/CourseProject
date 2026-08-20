using CourseProject.Application.Models;
using CourseProject.Presentation.Interfaces;
using CourseProject.Presentation.Models;

namespace CourseProject.Presentation.Services
{
    public class BookingModelDtoMapperService : IBookingModelDtoMapperService
    {
        public BookingDto ModelToDto(BookingModel bookingModel)
        {
            BookingDto bookingDto = new(bookingModel.Id, bookingModel.EventId, bookingModel.UserId, (CourseProject.Application.Models.BookingStatus)bookingModel.Status, bookingModel.CreatedAt)
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
                Status = (CourseProject.Presentation.Models.BookingStatus)bookingDto.Status,
                CreatedAt = bookingDto.CreatedAt,
                ProcessedAt = bookingDto.ProcessedAt
            };
            return bookingModel;
        }

    }
}
