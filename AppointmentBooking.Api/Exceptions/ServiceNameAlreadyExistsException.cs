namespace AppointmentBooking.Api.Exceptions
{
    public sealed class ServiceNameAlreadyExistsException : Exception
    {
        public ServiceNameAlreadyExistsException(string name, Exception innerException) : base($"A service with name '{name}' already exists", innerException) { }
    }
}
