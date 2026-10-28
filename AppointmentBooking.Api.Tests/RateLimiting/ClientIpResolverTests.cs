using System.Net;
using AppointmentBooking.Api.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace AppointmentBooking.Api.Tests.RateLimiting
{
    public sealed class ClientIpResolverTests
    {
        private const string Header = "X-Real-IP";

        [Fact]
        public void GetPartitionKey_UsesTrustedHeader_WhenConfigured()
        {
            DefaultHttpContext context = CreateContext("10.0.0.1", headerValue: "85.10.1.5");

            ClientIpResolver.GetPartitionKey(context, Header).Should().Be("85.10.1.5");
        }

        [Fact]
        public void GetPartitionKey_IgnoresHeader_WhenNotConfigured()
        {
            DefaultHttpContext context = CreateContext("10.0.0.1", headerValue: "85.10.1.5");

            ClientIpResolver.GetPartitionKey(context, trustedClientIpHeader: null).Should().Be("10.0.0.1");
        }

        [Fact]
        public void GetPartitionKey_FallsBackToConnection_WhenHeaderIsInvalid()
        {
            DefaultHttpContext context = CreateContext("10.0.0.1", headerValue: "not-an-ip");

            ClientIpResolver.GetPartitionKey(context, Header).Should().Be("10.0.0.1");
        }

        [Fact]
        public void GetPartitionKey_GroupsIpv6ByNetwork()
        {
            DefaultHttpContext first = CreateContext("2001:db8:1:2:aaaa::1");
            DefaultHttpContext second = CreateContext("2001:db8:1:2:bbbb::2");

            ClientIpResolver.GetPartitionKey(first, null)
                .Should().Be(ClientIpResolver.GetPartitionKey(second, null))
                .And.EndWith("/64");
        }

        [Fact]
        public void GetPartitionKey_MapsIpv4MappedIpv6ToIpv4()
        {
            DefaultHttpContext context = CreateContext("::ffff:85.10.1.5");

            ClientIpResolver.GetPartitionKey(context, null).Should().Be("85.10.1.5");
        }

        [Fact]
        public void GetPartitionKey_ReturnsUnknown_WhenNoAddress()
        {
            DefaultHttpContext context = new();

            ClientIpResolver.GetPartitionKey(context, Header).Should().Be(ClientIpResolver.Unknown);
        }

        [Fact]
        public void GetMaskedAddress_HidesLastIpv4Octet()
        {
            DefaultHttpContext context = CreateContext("85.10.1.5");

            ClientIpResolver.GetMaskedAddress(context, null).Should().Be("85.10.1.0");
        }

        [Fact]
        public void GetMaskedAddress_KeepsOnlyIpv6Prefix()
        {
            DefaultHttpContext context = CreateContext("2001:db8:1:2:aaaa::1");

            ClientIpResolver.GetMaskedAddress(context, null).Should().Be("2001:db8:1::/48");
        }

        private static DefaultHttpContext CreateContext(string remoteAddress, string? headerValue = null)
        {
            DefaultHttpContext context = new();
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteAddress);
            if (headerValue is not null)
            {
                context.Request.Headers[Header] = headerValue;
            }

            return context;
        }
    }
}
