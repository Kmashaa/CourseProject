using CourseProject.Entities;
using CourseProject.Interfaces;
using CourseProject.Models;

namespace CourseProject.Services
{
    public class BookingDtoMapperService : IBookingDtoMapperService
    {
        public Booking DtoToEntity(BookingDto bookingDto)
        {
            Booking booking = new()
            {
                Id = bookingDto.Id,
                EventId = bookingDto.EventId,
                Status = (Entities.BookingStatus)bookingDto.Status,
                CreatedAt = bookingDto.CreatedAt,
                ProcessedAt = bookingDto.ProcessedAt
            };
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
