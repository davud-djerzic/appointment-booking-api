namespace AppointmentBooking.Api.Exceptions
{
    public sealed class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message)
        {
        }
    }
}
