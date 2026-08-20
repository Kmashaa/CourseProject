namespace OrderConsumer.Contracts;

public record BookingCreated
{
    public const string TopicName = "booking-created";

    public Guid BookingId { get; init; }

    public Guid EventId { get; init; }

    public Guid UserId { get; init; }

    public int NumOfSeats {  get; init; }

    public DateTime CreatedAt { get; init; }
}
