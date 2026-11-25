using BookingApi.Entities;

namespace BookingApi.Dtos;

public class HistoryResponse
{
    public BookingStatus? FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }
    public Actor Actor { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
