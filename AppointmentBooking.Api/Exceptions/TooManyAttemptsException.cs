namespace AppointmentBooking.Api.Exceptions
{
    /// <summary>
    /// 429 with Retry-After: too many attempts for one email address (login, reset code request).
    /// Same response shape as the rate limiter, so the mobile app shows "Previše pokušaja".
    /// </summary>
    public sealed class TooManyAttemptsException(TimeSpan retryAfter)
        : Exception("Too many attempts. Please try again later.")
    {
        public TimeSpan RetryAfter { get; } = retryAfter;
    }
}
