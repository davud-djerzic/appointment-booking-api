namespace AppointmentBooking.Api.DTOs.Auth.Response
{
    public sealed record RegisterCustomerResponse(long CustomerId, string FirstName, string LastName, string Email);
}
