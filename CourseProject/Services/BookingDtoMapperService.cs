using CourseProject.Entities;
using CourseProject.Interfaces;
using CourseProject.Models;

namespace CourseProject.Services
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
