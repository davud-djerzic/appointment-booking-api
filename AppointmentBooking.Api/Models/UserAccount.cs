using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.Models
{
    public sealed class UserAccount
    {
        public long Id { get; init; }

        public string FirstName { get; init; } = null!;

        public string LastName { get; init; } = null!;

        public string Email { get; init; } = null!;

        public string PasswordHash { get; set; }

        public UserRole Role { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset UpdatedAt { get; init; }
    }
}
