using System.Threading.RateLimiting;

namespace AppointmentBooking.Api.RateLimiting
{
    /// <summary>
    /// Limits how many requests that hash or check a password run at the same time for the
    /// whole API: sign-in, registration, reset code (request, verify, new password), change
    /// password and delete account. Password hashing is slow on purpose
    /// (about 0.1 s of CPU each). If many arrive at once (e.g. an attack from many IP
    /// addresses), only a few use the CPU and the rest of the app (bookings, staff panel)
    /// stays responsive. 4 at a time allows about 40 such requests per second.
    /// Other requests of signed-in users (bookings, profile, staff panel) do not go through it.
    /// </summary>
    public sealed class PasswordHashingLimiter : IDisposable
    {
        public const int PermitLimit = 4;

        // A short queue, so a real user who happens to arrive at the same moment
        // waits a fraction of a second instead of getting an error.
        public const int QueueLimit = 20;

        private readonly ConcurrencyLimiter limiter = new(new ConcurrencyLimiterOptions
        {
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

        public ValueTask<RateLimitLease> AcquireAsync(CancellationToken cancellationToken) =>
            limiter.AcquireAsync(1, cancellationToken);

        public void Dispose() => limiter.Dispose();
    }
}
