using CourseProject.Domain.Entities;
using CourseProject.Application.Interfaces;
using CourseProject.Application.Models;

namespace CourseProject.Application.Services
{
    public class BookingDtoMapperService : IBookingDtoMapperService
    {
        public Booking DtoToEntity(BookingDto bookingDto)
        {
            Booking booking = new(bookingDto.Id, bookingDto.EventId, (Domain.Entities.BookingStatus)bookingDto.Status, bookingDto.CreatedAt) { };

            return booking;
        }

        public BookingDto EntityToDto(Booking booking)
        {
            BookingDto bookingDto = new(booking.Id, booking.EventId, (Application.Models.BookingStatus)booking.Status, booking.CreatedAt)
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
