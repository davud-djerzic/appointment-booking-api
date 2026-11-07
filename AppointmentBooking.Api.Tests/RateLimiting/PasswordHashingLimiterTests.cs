using System.Threading.RateLimiting;
using AppointmentBooking.Api.RateLimiting;
using FluentAssertions;

namespace AppointmentBooking.Api.Tests.RateLimiting
{
    public sealed class PasswordHashingLimiterTests
    {
        [Fact]
        public async Task AllowsFourAtOnce_QueuesTwenty_RejectsTheRest()
        {
            using PasswordHashingLimiter limiter = new();

            List<RateLimitLease> running = [];
            for (int i = 0; i < PasswordHashingLimiter.PermitLimit; i++)
            {
                running.Add(await limiter.AcquireAsync(CancellationToken.None));
            }

            List<Task<RateLimitLease>> queued = [];
            for (int i = 0; i < PasswordHashingLimiter.QueueLimit; i++)
            {
                queued.Add(limiter.AcquireAsync(CancellationToken.None).AsTask());
            }

            using RateLimitLease rejected = await limiter.AcquireAsync(CancellationToken.None);

            running.Should().OnlyContain(lease => lease.IsAcquired);
            queued.Should().OnlyContain(task => !task.IsCompleted);
            rejected.IsAcquired.Should().BeFalse();

            // A finished request frees its slot for the oldest waiting one.
            running[0].Dispose();
            using RateLimitLease firstQueued = await queued[0];
            firstQueued.IsAcquired.Should().BeTrue();

            foreach (RateLimitLease lease in running.Skip(1))
            {
                lease.Dispose();
            }

            for (int i = 1; i < queued.Count; i++)
            {
                using RateLimitLease lease = await queued[i];
                lease.IsAcquired.Should().BeTrue();
            }
        }
    }
}
