using AppointmentBooking.Api.DTOs.Salons.Request;
using AppointmentBooking.Api.DTOs.Salons.Response;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.Salons;
using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.Services.Salons
{
    public sealed class SalonService(ISalonRepository salonRepository) : ISalonService
    {
        public async Task<SalonResponse> CreateAsync(CreateSalonRequest request,CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Salon salon = new()
                {
                    Name = request.Name.Trim(),
                    Address = request.Address.Trim(),
                    City = request.City.Trim(),
                    InstagramUrl = NormalizeOptional(request.InstagramUrl),
                    FacebookUrl = NormalizeOptional(request.FacebookUrl),
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    IsActive = request.IsActive
                };

            SalonWorkingHours[] workingHours =
                request.WorkingHours
                    .Select(workingHour => new SalonWorkingHours
                    {
                        DayOfWeek = workingHour.DayOfWeek,
                        StartsAt = workingHour.StartsAt,
                        EndsAt = workingHour.EndsAt
                    })
                    .ToArray();

            SalonWithWorkingHours created =
                await salonRepository.CreateAsync(
                    salon,
                    workingHours,
                    cancellationToken);

                return MapToResponse(created);
            }

        public async Task<SalonResponse> GetByIdAsync(
            long salonId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SalonWithWorkingHours? salonWithWorkingHours =
                await salonRepository.GetByIdAsync(
                    salonId,
                    cancellationToken);

            if (salonWithWorkingHours is null)
            {
                throw new NotFoundException(
                    $"Salon with ID '{salonId}' was not found.");
            }

            return MapToResponse(salonWithWorkingHours);
        }

        private static SalonResponse MapToResponse(
            SalonWithWorkingHours salonWithWorkingHours)
        {
            Salon salon = salonWithWorkingHours.Salon;

            return new SalonResponse(
                salon.Id,
                salon.Name,
                salon.Address,
                salon.City,
                salon.InstagramUrl,
                salon.FacebookUrl,
                salon.Latitude,
                salon.Longitude,
                salon.IsActive,
                salonWithWorkingHours.WorkingHours
                    .Select(workingHour =>
                        new SalonWorkingHoursResponse(
                            workingHour.DayOfWeek,
                            workingHour.StartsAt,
                            workingHour.EndsAt))
                    .ToArray());
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
