using System.Text.Json;
using StackExchange.Redis;
using AppointmentBooking.Api.Models;


namespace AppointmentBooking.Api.Repositories.BookingHolds
{
    public sealed class BookingHoldRepository(IConnectionMultiplexer redis) : IBookingHoldRepository
    {
        private readonly IDatabase database = redis.GetDatabase();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private const string CreateHoldScript = """
            local activeKey = KEYS[1]
            local expiryKey = KEYS[2]
            local holdKey = KEYS[3]

            local token = ARGV[1]
            local startsAtMs = tonumber(ARGV[2])
            local endsAtMs = tonumber(ARGV[3])
            local nowMs = tonumber(ARGV[4])
            local expiresAtMs = tonumber(ARGV[5])
            local ttlMs = tonumber(ARGV[6])
            local holdJson = ARGV[7]

             -- Remove expired hold references.
            local expiredTokens =
                redis.call(
                    'ZRANGEBYSCORE',
                    expiryKey,
                    '-inf',
                    nowMs
                )

            for _, expiredToken in ipairs(expiredTokens) do
                local expiredHoldKey =
                    'booking:hold:' .. expiredToken

                redis.call(
                    'DEL',
                    expiredHoldKey
                )

                redis.call(
                    'ZREM',
                    activeKey,
                    expiredToken
                )

                redis.call(
                    'ZREM',
                    expiryKey,
                    expiredToken
                )
            end

            -- Find holds that can possibly overlap.
            local candidates =
                redis.call(
                    'ZRANGEBYSCORE',
                    activeKey,
                    '-inf',
                    '(' .. endsAtMs
                )

            for _, candidateToken in ipairs(candidates) do
                local candidateHoldKey =
                    'booking:hold:' .. candidateToken

                local existingEndsAt =
                    redis.call(
                        'HGET',
                        candidateHoldKey,
                        'endsAtMs'
                    )

                -- The hold expired naturally and its key is gone.
                if not existingEndsAt then
                    redis.call(
                        'ZREM',
                        activeKey,
                        candidateToken
                    )

                    redis.call(
                        'ZREM',
                        expiryKey,
                        candidateToken
                    )
                else
                    existingEndsAt =
                        tonumber(existingEndsAt)

                    if existingEndsAt > startsAtMs then
                        return 0
                    end
                end
            end

            -- A Guid collision should be practically impossible,
            -- but don't overwrite an existing hold.
            if redis.call(
                'EXISTS',
                holdKey
            ) == 1 then
                return -1
            end

            -- Store the actual hold.
            redis.call(
                'HSET',
                holdKey,
                'data',
                holdJson,
                'endsAtMs',
                endsAtMs
            )

            redis.call(
                'PEXPIRE',
                holdKey,
                ttlMs
            )

            -- Add the hold to the employee indexes.
            redis.call(
                'ZADD',
                activeKey,
                startsAtMs,
                token
            )

            redis.call(
                'ZADD',
                expiryKey,
                expiresAtMs,
                token
            )

            return 1
            """;

        private const string DeleteHoldScript = """
            local activeKey = KEYS[1]
            local expiryKey = KEYS[2]
            local holdKey = KEYS[3]

            local token = ARGV[1]

            redis.call(
                'DEL',
                holdKey
            )

            redis.call(
                'ZREM',
                activeKey,
                token
            )

            redis.call(
                'ZREM',
                expiryKey,
                token
            )

            return 1
            """;

        public async Task<bool> TryCreateAsync(
            BookingHold hold,
            TimeSpan ttl,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long startsAtMs =
                hold.StartsAt.ToUnixTimeMilliseconds();

            long endsAtMs =
                hold.EndsAt.ToUnixTimeMilliseconds();

            long nowMs =
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            long expiresAtMs =
                hold.ExpiresAt.ToUnixTimeMilliseconds();

            long ttlMs =
                checked((long)ttl.TotalMilliseconds);

            if (ttlMs <= 0)throw new ArgumentOutOfRangeException(nameof(ttl),"TTL must be greater than zero.");
            

            string holdJson =
                JsonSerializer.Serialize(
                    hold,
                    JsonOptions);

            RedisKey activeKey =
                $"booking:holds:employee:{hold.EmployeeId}:active";

            RedisKey expiryKey =
                $"booking:holds:employee:{hold.EmployeeId}:expiry";

            RedisKey holdKey =
                $"booking:hold:{hold.HoldToken}";

            RedisResult result =
                await database.ScriptEvaluateAsync(
                    CreateHoldScript,
                    new RedisKey[]
                    {
                        activeKey,
                        expiryKey,
                        holdKey
                    },
                    new RedisValue[]
                    {
                        hold.HoldToken.ToString(),
                        startsAtMs,
                        endsAtMs,
                        nowMs,
                        expiresAtMs,
                        ttlMs,
                        holdJson
                    });

            int resultCode = (int)result;

            return resultCode switch
            {
                1 => true,

                0 => false,

                -1 => throw new InvalidOperationException(
                    "The booking hold could not be created because its Redis key already exists."),

                _ => throw new InvalidOperationException(
                    $"Unexpected Redis booking hold result: {resultCode}.")
            };
        }

        public async Task<BookingHold?> GetAsync(Guid holdToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RedisKey holdKey = $"booking:hold:{holdToken}";

            RedisValue data = await database.HashGetAsync(holdKey, "data");

            if (!data.HasValue) return null;

            return JsonSerializer.Deserialize<BookingHold>(data.ToString(),JsonOptions);
        }

        public async Task DeleteAsync(BookingHold hold, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RedisKey activeKey = $"booking:holds:employee:{hold.EmployeeId}:active";

            RedisKey expiryKey = $"booking:holds:employee:{hold.EmployeeId}:expiry";

            RedisKey holdKey = $"booking:hold:{hold.HoldToken}";

            await database.ScriptEvaluateAsync(
                DeleteHoldScript,
                new RedisKey[]
                {
                    activeKey,
                    expiryKey,
                    holdKey
                },
                new RedisValue[]
                {
                    hold.HoldToken.ToString()
                });
        }

        public async Task<IReadOnlyList<BookingHold>> GetActiveForEmployeeAsync(long employeeId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RedisKey activeKey = $"booking:holds:employee:{employeeId}:active";

            long toMs = to.ToUnixTimeMilliseconds();

            RedisValue[] tokens = await database.SortedSetRangeByScoreAsync(activeKey, double.NegativeInfinity, toMs, Exclude.None, Order.Ascending);

            if (tokens.Length == 0) return Array.Empty<BookingHold>();

            List<BookingHold> holds = new(tokens.Length);

            foreach (RedisValue tokenValue in tokens)
            {
                if (!Guid.TryParse(tokenValue.ToString(), out Guid holdToken))
                    continue;

                BookingHold? hold = await GetAsync(holdToken, cancellationToken);

                if (hold is null) continue;

                if (hold.ExpiresAt <= DateTimeOffset.UtcNow) continue;

                if (hold.StartsAt < to && hold.EndsAt > from) holds.Add(hold);
            }

            return holds;
        }

    }
}
