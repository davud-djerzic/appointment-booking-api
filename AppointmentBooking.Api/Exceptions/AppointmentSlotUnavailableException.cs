namespace AppointmentBooking.Api.Exceptions
{
    public sealed class AppointmentSlotUnavailableException : Exception
    {
        public AppointmentSlotUnavailableException(Exception innerException) : base("The selected appointment slot is no longer available.", innerException) { }
    }
}
