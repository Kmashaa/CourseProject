using CourseProject.Interfaces;
using static System.Net.WebRequestMethods;

namespace CourseProject.Services
{
    public class BookingProcessingService : BackgroundService
    {
        private readonly IBookingRepository _repository;

        public BookingProcessingService(IBookingRepository repository)
        {
            _repository = repository;
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
                            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                            pendingBooking.Status = Entities.BookingStatus.Confirmed;
                            pendingBooking.ProcessedAt = DateTime.Now;
                            await _repository.UpdateAsync(pendingBooking);
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
