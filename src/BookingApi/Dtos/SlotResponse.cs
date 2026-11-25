namespace BookingApi.Dtos;

public class SlotResponse
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public int SeatsRemaining { get; set; }
}
