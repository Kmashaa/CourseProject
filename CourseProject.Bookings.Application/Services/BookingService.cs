using CourseProject.Bookings.Application.Interfaces;
using CourseProject.Bookings.Application.Interfaces;
using CourseProject.Bookings.Application.Models;
using CourseProject.Bookings.Domain.Entities;
using CourseProject.Bookings.Domain.Exceptions;
using Microsoft.Extensions.Configuration;

namespace CourseProject.Bookings.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly IBookingRepository _bookingRepository;
        private readonly IBookingDtoMapperService _bookingDtoMapperService;
        private readonly IConfiguration _configuration;
        private readonly IBookingCreatedProducer _bookingCreatedProducer;

        public BookingService(IBookingRepository bookingRepository, IBookingDtoMapperService bookingDtoMapperService, IConfiguration configuration, IBookingCreatedProducer bookingCreatedProducer)
        {
            _bookingRepository = bookingRepository;
            _bookingDtoMapperService = bookingDtoMapperService;
            _configuration = configuration;
            _bookingCreatedProducer = bookingCreatedProducer;
        }


        public async Task<BookingDto?> CreateBookingAsync(Guid? eventId, Guid userId)
        {
            if (eventId is not Guid validEventId)
            {
                throw new InvalidBookingDataException();
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
                
                var booking = Booking.CreatePending(validEventId, userId);

                await _bookingRepository.CreateAsync(booking);
                await _bookingCreatedProducer.PublishBookingCreated(booking.Id, validEventId, userId);

                return _bookingDtoMapperService.EntityToDto(booking);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<BookingDto?> GetBookingByIdAsync(Guid bookingId, Guid userId, string role)
        {

            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null)
            {
                throw new BookingNotFoundException(booking, "Booking not found");

            }

            //Enum.TryParse<Domain.Entities.Roles>(role, ignoreCase: true, out Domain.Entities.Roles userRole);

            //if (!(userRole == Domain.Entities.Roles.Admin || booking.UserId == userId))
            //{
            //    throw new NoPermissionException(userId);
            //}

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
                throw new BookingNotFoundException(booking, "Booking not found"); //temp


   
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
