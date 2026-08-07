using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Extension
{
    public static class AppointmentStatusExtensions
    {
        public static string ToDatabaseValue(this AppointmentStatus status)
        {
            return status.ToString().ToLowerInvariant();
        }

        public static AppointmentStatus ToAppointmentStatus(this string value)
        {
            return Enum.Parse<AppointmentStatus>(value, ignoreCase: true);
        }
    }
}
