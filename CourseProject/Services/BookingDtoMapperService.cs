using CourseProject.Presentation.Entities;
using CourseProject.Presentation.Interfaces;
using CourseProject.Presentation.Models;

namespace CourseProject.Presentation.Services
{
    public class BookingDtoMapperService : IBookingDtoMapperService
    {
        public Booking DtoToEntity(BookingDto bookingDto)
        {
            Booking booking = new(bookingDto.Id, bookingDto.EventId, (Entities.BookingStatus)bookingDto.Status, bookingDto.CreatedAt) { };

            return booking;
        }

        public BookingDto EntityToDto(Booking booking)
        {
            BookingDto bookingDto = new()
            {
                Id = booking.Id,
                EventId = booking.EventId,
                Status = (Models.BookingStatus)booking.Status,
                CreatedAt = booking.CreatedAt,
                ProcessedAt = booking.ProcessedAt
            };
            return bookingDto;
        }

    }
}
