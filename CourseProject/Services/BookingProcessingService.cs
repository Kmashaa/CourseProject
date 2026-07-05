using CourseProject.Entities;
using CourseProject.Interfaces;

namespace CourseProject.Services
{
    public class BookingProcessingService : BackgroundService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ILogger<BookingProcessingService> _logger;

        private readonly SemaphoreSlim _processingSemaphore = new SemaphoreSlim(1, 1);

        public BookingProcessingService(IBookingRepository bookingRepository, IEventRepository eventRepository, ILogger<BookingProcessingService> logger)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var pendingBookings = await _bookingRepository.GetPendingsAsync();

                    stoppingToken.ThrowIfCancellationRequested();

                    if (pendingBookings != null)
                    {
                        var tasks = pendingBookings.Select(b => ProcessBookingAsync(b, stoppingToken));
                        await Task.WhenAll(tasks);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            _logger.LogInformation($"{DateTime.Now}: Заявка {booking.Id} взята в обработку");

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            await _processingSemaphore.WaitAsync();

            try
            {
                var @event = _eventRepository.GetById(booking.EventId);

                stoppingToken.ThrowIfCancellationRequested();

                if (@event == null)
                {
                    booking.Reject();
                    await _bookingRepository.UpdateAsync(booking);
                    _logger.LogWarning($"{DateTime.Now}: Заявка {booking.Id} отклонена");
                }
                else
                {
                    booking.Confirm();
                    await _bookingRepository.UpdateAsync(booking);
                    _logger.LogInformation($"{DateTime.Now}: Заявка {booking.Id} обработана");

                }
            }
            catch
            {
                var @event = _eventRepository.GetById(booking.EventId);

                booking.Reject();
                @event.ReleaseSeats();
                await _bookingRepository.UpdateAsync(booking);
                _eventRepository.Update(@event);
                _logger.LogWarning($"{DateTime.Now}: Заявка {booking.Id} отклонена");

            }
            finally
            {
                _processingSemaphore.Release();
            }


        }
    }
}
