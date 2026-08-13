using CourseProject.DataAccess;
using CourseProject.Entities;
using CourseProject.Interfaces;
using System.Runtime.InteropServices;

namespace CourseProject.Services
{
    public class BookingProcessingService : BackgroundService
    {

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingProcessingService> _logger;

        private readonly SemaphoreSlim _processingSemaphore = new SemaphoreSlim(1, 1);

        private readonly int PollingInterval = 2;
        private readonly int ProcessingDelay = 5;

        public BookingProcessingService(IServiceScopeFactory scopeFactory, ILogger<BookingProcessingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    List<Guid> pendingBookings;
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                        pendingBookings = await bookingRepository.GetPendingsAsync();
                    }

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
                await Task.Delay(TimeSpan.FromSeconds(PollingInterval), stoppingToken);
            }
        }

        private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            _logger.LogInformation($"{DateTime.Now}: Заявка {bookingId} взята в обработку");

            await Task.Delay(TimeSpan.FromSeconds(ProcessingDelay), stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

            try
            {

                var booking = await bookingRepository.GetByIdAsync(bookingId);
                var @event = await eventRepository.GetByIdAsync(booking.EventId);

                stoppingToken.ThrowIfCancellationRequested();

                if (@event == null)
                {
                    booking.Reject();
                    await bookingRepository.UpdateAsync(booking);
                    //await context.SaveChangesAsync(stoppingToken);
                    _logger.LogWarning($"{DateTime.Now}: Заявка {booking.Id} отклонена");
                }
                else
                {
                    booking.Confirm();
                    await bookingRepository.UpdateAsync(booking);
                    //await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"{DateTime.Now}: Заявка {booking.Id} обработана");

                }
            }
            catch
            {
                var booking = await bookingRepository.GetByIdAsync(bookingId);
                var @event = await eventRepository.GetByIdAsync(booking.EventId);


                booking.Reject();
                @event?.ReleaseSeats();
                //await context.SaveChangesAsync(stoppingToken);
                bookingRepository.UpdateAsync(booking);
                eventRepository.UpdateAsync(@event);
                _logger.LogWarning($"{DateTime.Now}: Заявка {booking.Id} отклонена");

            }
            finally
            {
                _processingSemaphore.Release();
            }


        }
    }
}
