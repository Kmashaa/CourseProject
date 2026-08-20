using CourseProject.Bookings.Domain.Entities;
using CourseProject.Bookings.Application.Interfaces;
using CourseProject.Bookings.Application.Models;

namespace CourseProject.Bookings.Application.Services
{
    public class BookingDtoMapperService : IBookingDtoMapperService
    {
        public Booking DtoToEntity(BookingDto bookingDto)
        {
            Booking booking = new(bookingDto.Id, bookingDto.EventId, bookingDto.UserId, (Domain.Entities.BookingStatus)bookingDto.Status, bookingDto.CreatedAt) { };

            return booking;
        }

        public BookingDto EntityToDto(Booking booking)
        {
            BookingDto bookingDto = new(booking.Id, booking.EventId, booking.UserId, (Application.Models.BookingStatus)booking.Status, booking.CreatedAt)
            {
                Id = booking.Id,
                EventId = booking.EventId,
                UserId = booking.UserId,
                Status = (Models.BookingStatus)booking.Status,
                CreatedAt = booking.CreatedAt,
                ProcessedAt = booking.ProcessedAt
            };
            return bookingDto;
        }

    }
}
