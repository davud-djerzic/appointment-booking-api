namespace AppointmentBooking.Api.Repositories
{
    public interface IDatabaseHealthRepository
    {
        Task<bool> CanConnectAsync(CancellationToken cancellationToken);
    }
}
