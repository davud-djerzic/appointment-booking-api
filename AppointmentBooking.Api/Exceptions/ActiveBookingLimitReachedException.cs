namespace AppointmentBooking.Api.Exceptions
{
    /// <summary>
    /// 409: the customer already has the maximum number of upcoming bookings
    /// (Bookings:MaxActiveBookingsPerCustomer). The mobile app recognises this message.
    /// </summary>
    public sealed class ActiveBookingLimitReachedException(int limit)
        : ConflictException($"The customer already has the maximum of {limit} upcoming bookings.");
}
