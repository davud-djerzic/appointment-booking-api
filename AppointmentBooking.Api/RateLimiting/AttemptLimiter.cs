using System.Security.Cryptography;
using System.Text;

namespace AppointmentBooking.Api.RateLimiting
{
    /// <summary>A limit per email address, e.g. 5 login attempts in 15 minutes.</summary>
    public sealed record AttemptPolicy(string Name, int PermitLimit, TimeSpan Window)
    {
        // Password guessing against one account, from any number of IP addresses.
        public static AttemptPolicy Login { get; } = new("login", 5, TimeSpan.FromMinutes(15));

        // Reset codes sent to one address (protects the inbox and the Mailjet quota).
        public static AttemptPolicy PasswordResetRequest { get; } = new("password-reset-request", 3, TimeSpan.FromMinutes(15));
    }

    public readonly record struct AttemptResult(bool IsAllowed, TimeSpan RetryAfter);

    public interface IAttemptLimiter
    {
        /// <summary>Counts one attempt for the email; returns IsAllowed = false once the limit is reached.</summary>
        AttemptResult TryAttempt(AttemptPolicy policy, string email);

        /// <summary>Forgets the attempts for the email (e.g. after a successful login).</summary>
        void Reset(AttemptPolicy policy, string email);
    }

    /// <summary>
    /// Counts attempts per email address, so people who share a public IP address
    /// (salon Wi-Fi, mobile operators) do not block each other.
    /// Counters live in the API process memory (one instance on Railway) and are lost on restart.
    /// Emails are kept only as SHA-256 hashes. The window starts with the first attempt and
    /// is not extended by later attempts, so a block always ends after at most one window.
    /// </summary>
    public sealed class AttemptLimiter(TimeProvider timeProvider) : IAttemptLimiter
    {
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);

        // Memory guard if someone sends attempts for a huge number of different emails
        // (the per-IP limits already slow this down). Beyond it new emails are not tracked.
        private const int MaxTrackedEntries = 50_000;

        private readonly Dictionary<string, AttemptWindow> windows = [];
        private readonly Lock gate = new();
        private DateTimeOffset nextCleanupAt = DateTimeOffset.MinValue;

        public AttemptResult TryAttempt(AttemptPolicy policy, string email)
        {
            string key = CreateKey(policy, email);
            DateTimeOffset now = timeProvider.GetUtcNow();

            lock (gate)
            {
                RemoveExpiredWindows(now);

                if (!windows.TryGetValue(key, out AttemptWindow? window) || window.ExpiresAt <= now)
                {
                    if (windows.Count >= MaxTrackedEntries && !windows.ContainsKey(key))
                    {
                        return new AttemptResult(true, TimeSpan.Zero);
                    }

                    window = new AttemptWindow(now.Add(policy.Window));
                    windows[key] = window;
                }

                if (window.Count >= policy.PermitLimit)
                {
                    return new AttemptResult(false, window.ExpiresAt - now);
                }

                window.Count++;
                return new AttemptResult(true, TimeSpan.Zero);
            }
        }

        public void Reset(AttemptPolicy policy, string email)
        {
            string key = CreateKey(policy, email);

            lock (gate)
            {
                windows.Remove(key);
            }
        }

        private void RemoveExpiredWindows(DateTimeOffset now)
        {
            if (now < nextCleanupAt)
            {
                return;
            }

            nextCleanupAt = now.Add(CleanupInterval);

            List<string> expiredKeys = windows
                .Where(pair => pair.Value.ExpiresAt <= now)
                .Select(pair => pair.Key)
                .ToList();

            foreach (string expiredKey in expiredKeys)
            {
                windows.Remove(expiredKey);
            }
        }

        private static string CreateKey(AttemptPolicy policy, string email)
        {
            // Same as the database (CITEXT): "Mehmed@Gmail.com " and "mehmed@gmail.com" are one account.
            string normalizedEmail = email.Trim().ToLowerInvariant();
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail));

            return $"{policy.Name}:{Convert.ToHexString(hash)}";
        }

        private sealed class AttemptWindow(DateTimeOffset expiresAt)
        {
            public DateTimeOffset ExpiresAt { get; } = expiresAt;

            public int Count { get; set; }
        }
    }
}
