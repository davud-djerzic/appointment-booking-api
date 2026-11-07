using System.Threading.Channels;

namespace AppointmentBooking.Api.Services.Email
{
    public sealed class EmailQueue(ILogger<EmailQueue> logger) : IEmailQueue
    {
        // Far more than this salon will ever need; protects memory if Mailjet is down for a long time.
        private const int Capacity = 100;

        private readonly Channel<EmailMessage> channel =
            Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true
            });

        public ChannelReader<EmailMessage> Reader => channel.Reader;

        public void Enqueue(EmailMessage message)
        {
            if (!channel.Writer.TryWrite(message))
            {
                // No address in the log (personal data).
                logger.LogError(
                    "The email queue is full. Email \"{Subject}\" was not sent.",
                    message.Subject);
            }
        }
    }
}
