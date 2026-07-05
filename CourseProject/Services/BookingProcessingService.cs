using CourseProject.Interfaces;

namespace CourseProject.Services
{
    public class BookingProcessingService : BackgroundService
    {
        private readonly IBookingRepository _repository;
        private readonly ILogger<BookingProcessingService> _logger;


        public BookingProcessingService(IBookingRepository repository, ILogger<BookingProcessingService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var bookings = await _repository.GetAllAsync();
                    var pendingBookings = bookings.Where(b => b.Status == Entities.BookingStatus.Pending).OrderBy(c => c.CreatedAt).ToList();
                    if (pendingBookings != null)
                    {
                        foreach (var pendingBooking in pendingBookings)
                        {
                            _logger.LogInformation($"{DateTime.Now}: Задача {pendingBooking.Id} взята в обработку");
                            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                            pendingBooking.Status = Entities.BookingStatus.Confirmed;
                            pendingBooking.ProcessedAt = DateTime.Now;
                            await _repository.UpdateAsync(pendingBooking);
                            _logger.LogInformation($"{DateTime.Now}: Задача {pendingBooking.Id} обработана");

                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
