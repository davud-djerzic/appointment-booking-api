using System.Net;
using System.Net.Sockets;

namespace AppointmentBooking.Api.RateLimiting
{
    /// <summary>
    /// Resolves the client address used as rate limiting key.
    /// IP addresses are personal data: they are kept only in memory, inside the rate limiter,
    /// for the duration of a time window, and only in masked form in logs.
    /// </summary>
    public static class ClientIpResolver
    {
        public const string Unknown = "unknown";

        /// <summary>
        /// Rate limiting key for the request. IPv6 clients are grouped by their /64 network,
        /// because a single IPv6 user usually controls a whole /64 and could otherwise rotate addresses.
        /// </summary>
        public static string GetPartitionKey(HttpContext context, string? trustedClientIpHeader)
        {
            IPAddress? address = GetClientAddress(context, trustedClientIpHeader);
            if (address is null)
            {
                return Unknown;
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                byte[] bytes = address.GetAddressBytes();
                Array.Clear(bytes, 8, 8);
                return new IPAddress(bytes) + "/64";
            }

            return address.ToString();
        }

        /// <summary>Masked address for logs: IPv4 a.b.c.0, IPv6 first 48 bits.</summary>
        public static string GetMaskedAddress(HttpContext context, string? trustedClientIpHeader)
        {
            IPAddress? address = GetClientAddress(context, trustedClientIpHeader);
            if (address is null)
            {
                return Unknown;
            }

            byte[] bytes = address.GetAddressBytes();
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                bytes[3] = 0;
                return new IPAddress(bytes).ToString();
            }

            Array.Clear(bytes, 6, 10);
            return new IPAddress(bytes) + "/48";
        }

        private static IPAddress? GetClientAddress(HttpContext context, string? trustedClientIpHeader)
        {
            if (!string.IsNullOrWhiteSpace(trustedClientIpHeader) &&
                context.Request.Headers.TryGetValue(trustedClientIpHeader, out var values))
            {
                // X-Real-IP holds one address; if a list is ever sent, the first entry is the client.
                string candidate = values.ToString().Split(',')[0].Trim();
                if (IPAddress.TryParse(candidate, out IPAddress? headerAddress))
                {
                    return Normalize(headerAddress);
                }
            }

            IPAddress? remoteAddress = context.Connection.RemoteIpAddress;
            return remoteAddress is null ? null : Normalize(remoteAddress);
        }

        private static IPAddress Normalize(IPAddress address) =>
            address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }
}
