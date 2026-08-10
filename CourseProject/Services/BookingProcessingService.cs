using CourseProject.DataAccess;
using CourseProject.Entities;
using CourseProject.Interfaces;

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
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        pendingBookings = context.Bookings
                            .Where(b => b.Status == BookingStatus.Pending)
                            .Select(o=>o.Id)
                            .ToList();
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
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {

                var booking = context.Bookings.FirstOrDefault(o => o.Id == bookingId);
                var @event = context.Events.FirstOrDefault(o=>o.Id==booking.EventId);

                stoppingToken.ThrowIfCancellationRequested();

                if (@event == null)
                {
                    booking.Reject();
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogWarning($"{DateTime.Now}: Заявка {booking.Id} отклонена");
                }
                else
                {
                    booking.Confirm();
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"{DateTime.Now}: Заявка {booking.Id} обработана");

                }
            }
            catch
            {
                var booking = context.Bookings.FirstOrDefault(o => o.Id == bookingId);
                var @event = context.Events.FirstOrDefault(o => o.Id == booking.EventId);


                booking.Reject();
                @event?.ReleaseSeats();
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogWarning($"{DateTime.Now}: Заявка {booking.Id} отклонена");

            }
            finally
            {
                _processingSemaphore.Release();
            }


        }
    }
}
