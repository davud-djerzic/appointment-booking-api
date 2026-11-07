using AppointmentBooking.Api.RateLimiting;
using FluentAssertions;

namespace AppointmentBooking.Api.Tests.RateLimiting
{
    public sealed class AttemptLimiterTests
    {
        private static readonly AttemptPolicy Policy = new("test", 3, TimeSpan.FromMinutes(15));

        [Fact]
        public void TryAttempt_AllowsPermitLimit_ThenBlocksUntilWindowEnds()
        {
            ManualTimeProvider time = new();
            AttemptLimiter limiter = new(time);

            for (int i = 0; i < 3; i++)
            {
                limiter.TryAttempt(Policy, "kupac@example.com").IsAllowed.Should().BeTrue();
            }

            time.Advance(TimeSpan.FromMinutes(5));
            AttemptResult blocked = limiter.TryAttempt(Policy, "kupac@example.com");

            blocked.IsAllowed.Should().BeFalse();
            blocked.RetryAfter.Should().Be(TimeSpan.FromMinutes(10));
        }

        [Fact]
        public void TryAttempt_AllowsAgain_AfterWindowEnds()
        {
            ManualTimeProvider time = new();
            AttemptLimiter limiter = new(time);

            for (int i = 0; i < 3; i++)
            {
                limiter.TryAttempt(Policy, "kupac@example.com");
            }

            time.Advance(TimeSpan.FromMinutes(15));

            limiter.TryAttempt(Policy, "kupac@example.com").IsAllowed.Should().BeTrue();
        }

        [Fact]
        public void TryAttempt_TreatsCaseAndSpacesAsTheSameEmail()
        {
            AttemptLimiter limiter = new(new ManualTimeProvider());

            limiter.TryAttempt(Policy, "kupac@example.com");
            limiter.TryAttempt(Policy, " Kupac@Example.com");
            limiter.TryAttempt(Policy, "KUPAC@EXAMPLE.COM ");

            limiter.TryAttempt(Policy, "kupac@example.com").IsAllowed.Should().BeFalse();
        }

        [Fact]
        public void TryAttempt_CountsEmailsAndPoliciesSeparately()
        {
            AttemptLimiter limiter = new(new ManualTimeProvider());
            AttemptPolicy otherPolicy = new("other", 3, TimeSpan.FromMinutes(15));

            for (int i = 0; i < 3; i++)
            {
                limiter.TryAttempt(Policy, "kupac@example.com");
            }

            limiter.TryAttempt(Policy, "drugi@example.com").IsAllowed.Should().BeTrue();
            limiter.TryAttempt(otherPolicy, "kupac@example.com").IsAllowed.Should().BeTrue();
        }

        [Fact]
        public void Reset_ForgetsTheAttempts()
        {
            AttemptLimiter limiter = new(new ManualTimeProvider());

            for (int i = 0; i < 3; i++)
            {
                limiter.TryAttempt(Policy, "kupac@example.com");
            }

            limiter.Reset(Policy, "Kupac@Example.com");

            limiter.TryAttempt(Policy, "kupac@example.com").IsAllowed.Should().BeTrue();
        }

        private sealed class ManualTimeProvider : TimeProvider
        {
            private DateTimeOffset now = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => now;

            public void Advance(TimeSpan duration) => now = now.Add(duration);
        }
    }
}
