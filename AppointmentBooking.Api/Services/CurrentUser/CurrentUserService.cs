using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AppointmentBooking.Api.Services.CurrentUser
{
    public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        public long UserAccountId
        {
            get
            {
                string? value =httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!long.TryParse(value, out long id)) throw new UnauthorizedException("Authenticated user ID is missing.");
                

                return id;
            }
        }

        public UserRole Role
        {
            get
            {
                string? value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

                if (!Enum.TryParse<UserRole>(value, ignoreCase: true, out UserRole role)) throw new UnauthorizedException("Authenticated user role is missing.");

                return role;
            }
        }
    }
}
