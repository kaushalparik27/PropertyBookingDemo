using System.ComponentModel.DataAnnotations;

namespace BookingApi.Dtos;

public class CreateSlotRequest
{
    [Required]
    public Guid? PropertyId { get; set; }

    [Required]
    public DateTimeOffset? StartsAt { get; set; }

    [Range(15, 120)]
    public int DurationMinutes { get; set; }

    [Range(1, 20)]
    public int Capacity { get; set; }
}
