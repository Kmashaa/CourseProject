using CourseProject.Application.Exceptions;
using CourseProject.Application.Interfaces;
using CourseProject.Application.Models;
using CourseProject.Domain.Entities;
using CourseProject.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace CourseProject.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly IEventService _eventService;

        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;

        private readonly IEventDtoMapperService _eventDtoMapperService;
        private readonly IBookingDtoMapperService _bookingDtoMapperService;


        public BookingService(IEventService eventService, IBookingRepository bookingRepository, IEventRepository eventRepository, IEventDtoMapperService eventDtoMapperService, IBookingDtoMapperService bookingDtoMapperService)
        {
            _eventService = eventService;
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _eventDtoMapperService = eventDtoMapperService;
            _bookingDtoMapperService = bookingDtoMapperService;
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
                const int maxUserBookings = 10; //TODO: config
                var userBookingsCount = await _bookingRepository.GetActiveBookingsCountByUserIdAsync(userId);

                if (userBookingsCount >= maxUserBookings)
                {
                    throw new ActiveBookingsLimit(userId);
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

        public async Task<BookingDto?> GetBookingByIdAsync(Guid bookingId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new BookingNotFoundException(booking, "Booking not found");

            }
            return _bookingDtoMapperService.EntityToDto(booking);
        }

        public async Task<bool> CancelBookingAsync(Guid bookingId, Guid userId, Domain.Entities.Roles role=Domain.Entities.Roles.Admin) //TODO get role
        {


            await _semaphore.WaitAsync(); //semaphore, а не lock, т.к. внутри асинхронный код

            try
            {
                var booking = await _bookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    throw new BookingNotFoundException(booking, "Booking not found");

                }


                if (!(role == Domain.Entities.Roles.Admin || booking.UserId == userId))
                {
                    throw new NoPermissionException();
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
                    return true;
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
