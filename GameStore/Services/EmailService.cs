using GameStore.Interfaces;
using GameStore.Models;
using MimeKit;

namespace GameStore.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOrderConfirmationAsync(Order order)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Game Store", _configuration["Email:Sender"]));
            message.To.Add(new MailboxAddress("", order.Email));
            message.Subject = "Покупка игры - подтверждение заказа";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <h1>Спасибо за вашу покупку!</h1>
                    <p>Заказ №: {order.Id}</p>
                    <p>Дата заказа: {order.OrderDate:dd.MM.yyyy HH:mm}</p>
                    <p>Игра: {order.Game?.Title}</p>
                    <p>Цена: {order.Game?.Price} ₽</p>
                    <p>Ключ активации: <strong>{order.Key}</strong></p>
                    <p>Спасибо за использование наших услуг!</p>
                "
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(
                _configuration["Email:Host"] ?? throw new ArgumentNullException("Email:Host is not configured."),
                int.Parse(_configuration["Email:Port"] ?? throw new ArgumentNullException("Email:Port is not configured.")),
                false
            );
            await client.AuthenticateAsync(_configuration["Email:Username"], _configuration["Email:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendSteamTopUpConfirmationAsync(SteamTopUp topUp)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Game Store", _configuration["Email:Sender"]));
            message.To.Add(new MailboxAddress("", topUp.Email));
            message.Subject = "Пополнение Steam - подтверждение";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <h1>Пополнение Steam выполнено!</h1>
                    <p>ID операции: {topUp.Id}</p>
                    <p>Дата: {topUp.Date:dd.MM.yyyy HH:mm}</p>
                    <p>Steam ID: {topUp.SteamId}</p>
                    <p>Сумма: {topUp.Amount} ₽</p>
                    <p>Спасибо за использование наших услуг!</p>
                "
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(
                _configuration["Email:Host"] ?? throw new ArgumentNullException("Email:Host is not configured."),
                int.Parse(_configuration["Email:Port"] ?? throw new ArgumentNullException("Email:Port is not configured.")),
                false
            );
            await client.AuthenticateAsync(_configuration["Email:Username"], _configuration["Email:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}