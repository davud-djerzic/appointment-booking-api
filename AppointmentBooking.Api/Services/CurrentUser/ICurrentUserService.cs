using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.Services.CurrentUser
{
    public interface ICurrentUserService
    {
        long UserAccountId { get; }
        UserRole Role { get;  }
    }
}
