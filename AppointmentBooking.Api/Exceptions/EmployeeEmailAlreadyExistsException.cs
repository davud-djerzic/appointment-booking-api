namespace AppointmentBooking.Api.Exceptions
{
    public sealed class EmployeeEmailAlreadyExistsException : Exception
    {
        // The address is not part of the message, so it never ends up in logs.
        public EmployeeEmailAlreadyExistsException(Exception innerException) : base("An employee with this email already exists.", innerException)
        {

        }
    }
}
