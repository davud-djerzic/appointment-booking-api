using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Employees.Request
{
    public sealed class GetEmployeesQuery
    {
        [StringLength(100)]
        public string? Search { get; init; }

        public bool? IsActive { get; init; }

        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 20;
    }
}
