namespace CourseProject.Contracts;

public record BookingCancelled
{
    public const string TopicName = "booking-cancelled";

    public Guid BookingId { get; init; }

    public Guid EventId { get; init; }

    public Guid UserId { get; init; }

    public int NumOfSeats {  get; init; }

    public DateTime CreatedAt { get; init; }
}
