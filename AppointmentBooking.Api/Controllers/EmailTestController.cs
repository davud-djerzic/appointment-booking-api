using AppointmentBooking.Api.Services.Email;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/dev/email-test")]
    public sealed class EmailTestController(
    IEmailService emailService,
    IHostEnvironment environment) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> SendTestEmail(
            CancellationToken cancellationToken)
        {
            if (!environment.IsDevelopment())
            {
                return NotFound();
            }

            await emailService.SendAsync(
                toEmail: "djerzicd831@gmail.com",
                toName: "Davud",
                subject: "Test email - Appointment Booking",
                textPart: """
                      Pozdrav Davud,

                      ovo je testni email iz Appointment Booking API-ja.

                      Mailjet integracija radi ispravno.

                      Lijep pozdrav,
                      Appointment Booking
                      """,
                htmlPart: """
                      <h2>Appointment Booking</h2>
                      <p>Pozdrav Davud,</p>
                      <p>ovo je testni email iz Appointment Booking API-ja.</p>
                      <p><strong>Mailjet integracija radi ispravno.</strong></p>
                      <p>Lijep pozdrav,<br/>Appointment Booking</p>
                      """,
                cancellationToken);

            return Ok(new
            {
                message = "Testni email je uspješno poslan."
            });
        }
    }
}
