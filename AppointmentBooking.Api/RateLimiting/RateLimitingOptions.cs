namespace AppointmentBooking.Api.RateLimiting
{
    /// <summary>
    /// Configuration section "RateLimiting".
    /// </summary>
    public sealed class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";

        /// <summary>
        /// Request header that contains the real client IP address, set by a trusted reverse proxy.
        /// On Railway this is "X-Real-IP" (the Railway edge overwrites it, clients cannot spoof it):
        /// environment variable RateLimiting__ClientIpHeader=X-Real-IP.
        /// Leave empty when the API is reached directly (no proxy); the connection address is used then.
        /// Never set it when the API is reachable without the proxy, because clients could forge the header.
        /// </summary>
        public string? ClientIpHeader { get; init; }
    }
}
