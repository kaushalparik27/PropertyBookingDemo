using System.ComponentModel.DataAnnotations;
using BookingApi.Entities;

namespace BookingApi.Dtos;

public class TransitionRequest
{
    [Required]
    public BookingStatus? To { get; set; }

    [Required]
    public Actor? Actor { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}
