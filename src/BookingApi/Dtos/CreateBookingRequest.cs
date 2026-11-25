using System.ComponentModel.DataAnnotations;

namespace BookingApi.Dtos;

public class CreateBookingRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public string RenterEmail { get; set; } = "";

    [Range(1, 6)]
    public int PartySize { get; set; }
}
