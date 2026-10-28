namespace AppointmentBooking.Api.RateLimiting
{
    /// <summary>Names of the rate limiting policies used with [EnableRateLimiting].</summary>
    public static class RateLimitPolicies
    {
        public const string Login = "login";
        public const string Register = "register";
        public const string Refresh = "refresh";
        public const string ForgotPassword = "forgot-password";
        public const string ResetCode = "reset-code";
        public const string BookingHold = "booking-hold";
    }
}
