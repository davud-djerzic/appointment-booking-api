namespace AppointmentBooking.Api.Time;

/// <summary>
/// The salon operates in Bosnia and Herzegovina. Keep business dates and
/// working hours independent from the machine's configured time zone.
/// </summary>
public static class SalonTimeZone
{
    public static TimeZoneInfo Info { get; } = Resolve();

    public static DateOnly Today => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Info).DateTime);

    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(
        DateTimeOffset.UtcNow,
        Info);

    public static DateTimeOffset At(DateOnly date, TimeOnly time)
    {
        DateTime localDateTime = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(localDateTime, Info.GetUtcOffset(localDateTime));
    }

    public static DateTimeOffset At(DateTime localDateTime)
    {
        DateTime unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, Info.GetUtcOffset(unspecified));
    }

    private static TimeZoneInfo Resolve()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Sarajevo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        }
    }
}
