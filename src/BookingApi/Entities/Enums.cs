namespace BookingApi.Entities;

public enum BookingStatus
{
    Confirmed,
    Cancelled,
    Attended,
    NoShow
}

public enum Actor
{
    Owner,
    Renter
}
