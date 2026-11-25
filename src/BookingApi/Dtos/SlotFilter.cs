using System.ComponentModel.DataAnnotations;

namespace BookingApi.Dtos;

public class SlotFilter
{
    /// <summary>Only slots of this property.</summary>
    public Guid? PropertyId { get; set; }

    /// <summary>Only slots starting at or after this time.</summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>Only slots starting at or before this time.</summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>Page number, starting at 1.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>Slots per page, from 1 to 100.</summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
