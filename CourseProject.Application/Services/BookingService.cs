using CourseProject.Application.Exceptions;
using CourseProject.Application.Interfaces;
using CourseProject.Application.Models;
using CourseProject.Domain.Entities;
using CourseProject.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CourseProject.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IBookingDtoMapperService _bookingDtoMapperService;
        private readonly IConfiguration _configuration;



        public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository, IBookingDtoMapperService bookingDtoMapperService, IConfiguration configuration)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _bookingDtoMapperService = bookingDtoMapperService;
            _configuration = configuration;
        }


        public async Task<BookingDto?> CreateBookingAsync(Guid? eventId, Guid userId)
        {
            if (eventId == null)
            {
                throw new InvalidEventDataException();
            }

            await _semaphore.WaitAsync(); //semaphore, а не lock, т.к. внутри асинхронный код

            try
            {
                int maxUserBookings = Convert.ToInt32(_configuration["BookingsLimit"]); 
                var userBookingsCount = await _bookingRepository.GetActiveBookingsCountByUserIdAsync(userId);

                if (userBookingsCount >= maxUserBookings)
                {
                    throw new ActiveBookingsLimit(userId, maxUserBookings);
                }

                var @event = await _eventRepository.GetByIdAsync((Guid)eventId);
                if (@event == null)
                {
                    throw new EventNotFoundException((Guid)eventId);
                }
                if (@event.StartAt <= DateTime.Now)
                {
                    throw new PastEventException(@event.Id);
                }
                if (@event.TryReserveSeats())
                {
                    var booking = Booking.CreatePending(@event.Id, userId);
                    await _bookingRepository.CreateAsync(booking);
                    await _eventRepository.UpdateAsync(@event);
                    return _bookingDtoMapperService.EntityToDto(booking);
                }
                else
                {
                    throw new NoAvailableSeatsException();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<BookingDto?> GetBookingByIdAsync(Guid bookingId, Guid userId, string role)
        {

            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            Enum.TryParse<Domain.Entities.Roles>(role, ignoreCase: true, out Domain.Entities.Roles userRole);

            if (!(userRole == Domain.Entities.Roles.Admin || booking.UserId == userId))
            {
                throw new NoPermissionException(userId);
            }

            if (booking == null)
            {
                throw new BookingNotFoundException(booking, "Booking not found");

            }


            return _bookingDtoMapperService.EntityToDto(booking);
        }

        public async Task<BookingDto?> CancelBookingAsync(Guid bookingId, Guid userId, string role)
        {


            await _semaphore.WaitAsync(); //semaphore, а не lock, т.к. внутри асинхронный код

            try
            {
                var booking = await _bookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    throw new BookingNotFoundException(booking, "Booking not found");

                }
                Enum.TryParse<Domain.Entities.Roles>(role, ignoreCase: true, out Domain.Entities.Roles userRole);

                if (!(userRole == Domain.Entities.Roles.Admin || booking.UserId == userId))
                {
                    throw new NoPermissionException(userId);
                }

                var @event = await _eventRepository.GetByIdAsync((Guid)booking.EventId);

                if (@event == null)
                {
                    throw new EventNotFoundException((Guid)booking.EventId);
                }
                if (@event.StartAt <= DateTime.Now)
                {
                    throw new PastEventException(@event.Id);
                }



                if (@event.ReleaseSeats())
                {
                    booking.Cancel();
                    await _bookingRepository.UpdateAsync(booking);
                    await _eventRepository.UpdateAsync(@event);
                    return _bookingDtoMapperService.EntityToDto(booking);
                }
                else
                {
                    throw new InvalidEventDataException();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
