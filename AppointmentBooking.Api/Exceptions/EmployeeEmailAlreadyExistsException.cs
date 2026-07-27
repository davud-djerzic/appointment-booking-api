namespace AppointmentBooking.Api.Exceptions
{
    public sealed class EmployeeEmailAlreadyExistsException : Exception
    {
        public EmployeeEmailAlreadyExistsException(string email, Exception innerException) : base($"An employee with email '{email}' already exists", innerException)
        {

        }
    }
}
