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
                <!DOCTYPE html>
                <html lang='ru'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Подтверждение заказа</title>
                    <style>
                        body {{
                            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                            margin: 0;
                            padding: 0;
                            background-color: #1f2122;
                            color: #e5e5e5;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 0 auto;
                            background-color: #1f2122;
                            border-radius: 10px;
                            overflow: hidden;
                            box-shadow: 0 0 20px rgba(0, 0, 0, 0.1);
                        }}
                        .content {{
                            padding: 30px;
                            background-color: #27292a;
                            color: #e5e5e5;
                        }}
                        h1 {{
                            color: #ec6090;
                            margin-top: 0;
                            font-size: 24px;
                            font-weight: 700;
                            text-align: center;
                        }}
                        .order-details {{
                            background-color: #1f2122;
                            border-radius: 10px;
                            padding: 20px;
                            margin-top: 20px;
                            margin-bottom: 20px;
                        }}
                        .detail-row {{
                            margin-bottom: 15px;
                            padding-bottom: 15px;
                            border-bottom: 1px solid #444;
                        }}
                        .detail-label {{
                            color: #ec6090;
                            font-weight: bold;
                            margin-bottom: 5px;
                            display: block;
                        }}
                        .detail-value {{
                            font-size: 16px;
                            color: #e5e5e5;
                        }}
                        .key-highlight {{
                            background-color: #ec6090;
                            color: white;
                            padding: 15px;
                            text-align: center;
                            border-radius: 8px;
                            margin: 25px 0;
                            font-size: 20px;
                            font-weight: bold;
                            letter-spacing: 1px;
                        }}
                        .footer {{
                            background-color: #1f2122;
                            padding: 15px;
                            text-align: center;
                            font-size: 12px;
                            color: #999;
                            border-top: 1px solid #444;
                        }}
                        p {{
                            color: #e5e5e5;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='content'>
                            <h1>Спасибо за вашу покупку!</h1>
                            
                            <p>Здравствуйте!</p>
                            <p>Ваш заказ успешно оформлен и оплачен. Ниже вы найдете детали вашего заказа и ключ активации:</p>
                            
                            <div class='order-details'>
                                <div class='detail-row'>
                                    <div class='detail-label'>Заказ №:</div>
                                    <div class='detail-value'>{order.Id}</div>
                                </div>
                                
                                <div class='detail-row'>
                                    <div class='detail-label'>Дата заказа:</div>
                                    <div class='detail-value'>{order.OrderDate:dd.MM.yyyy HH:mm}</div>
                                </div>
                                
                                <div class='detail-row'>
                                    <div class='detail-label'>Игра:</div>
                                    <div class='detail-value'>{order.Game?.Title}</div>
                                </div>
                                
                                <div class='detail-row' style='border-bottom: none; margin-bottom: 0; padding-bottom: 0;'>
                                    <div class='detail-label'>Цена:</div>
                                    <div class='detail-value'>{order.Game?.Price} ₽</div>
                                </div>
                            </div>
                            
                            <div class='key-highlight'>
                                <div style='margin-bottom: 8px; font-size: 14px; color: rgba(255,255,255,0.8);'>Ваш ключ активации:</div>
                                {order.Key}
                            </div>
                            
                            <p>Для активации игры введите ключ в соответствующее поле в игровой платформе.</p>
                            <p>Если у вас возникнут вопросы, не стесняйтесь обращаться в нашу службу поддержки.</p>
                        </div>
                        
                        <div class='footer'>
                            <p>© {DateTime.Now.Year} Game Store. Все права защищены.</p>
                            <p>Если вы не делали этого заказа, пожалуйста, свяжитесь с нами.</p>
                        </div>
                    </div>
                </body>
                </html>
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
                <!DOCTYPE html>
                <html lang='ru'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Подтверждение пополнения Steam</title>
                    <style>
                        body {{
                            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                            margin: 0;
                            padding: 0;
                            background-color: #1f2122;
                            color: #e5e5e5;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 0 auto;
                            background-color: #1f2122;
                            border-radius: 10px;
                            overflow: hidden;
                            box-shadow: 0 0 20px rgba(0, 0, 0, 0.1);
                        }}
                        .steam-logo {{
                            background-color: #1f2122;
                            padding: 15px 0;
                            text-align: center;
                        }}
                        .steam-logo img {{
                            height: 40px;
                            width: auto;
                        }}
                        .content {{
                            padding: 30px;
                            background-color: #27292a;
                            color: #e5e5e5;
                        }}
                        h1 {{
                            color: #1b9bff;
                            margin-top: 0;
                            font-size: 24px;
                            font-weight: 700;
                            text-align: center;
                        }}
                        .topup-details {{
                            background-color: #1f2122;
                            border-radius: 10px;
                            padding: 20px;
                            margin-top: 20px;
                            margin-bottom: 20px;
                        }}
                        .detail-row {{
                            margin-bottom: 15px;
                            padding-bottom: 15px;
                            border-bottom: 1px solid #444;
                        }}
                        .detail-label {{
                            color: #1b9bff;
                            font-weight: bold;
                            margin-bottom: 5px;
                            display: block;
                        }}
                        .detail-value {{
                            font-size: 16px;
                            color: #e5e5e5;
                        }}
                        .amount-highlight {{
                            background-color: #1b9bff;
                            color: white;
                            padding: 15px;
                            text-align: center;
                            border-radius: 8px;
                            margin: 25px 0;
                            font-size: 26px;
                            font-weight: bold;
                        }}
                        .footer {{
                            background-color: #1f2122;
                            padding: 15px;
                            text-align: center;
                            font-size: 12px;
                            color: #999;
                            border-top: 1px solid #444;
                        }}
                        p {{
                            color: #e5e5e5;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='content'>
                            <div class='steam-logo'>
                                <img src='https://upload.wikimedia.org/wikipedia/commons/thumb/8/83/Steam_icon_logo.svg/2048px-Steam_icon_logo.svg.png' alt='Steam Logo'>
                            </div>
                            
                            <h1>Пополнение Steam выполнено!</h1>
                            
                            <p>Здравствуйте!</p>
                            <p>Ваш баланс Steam успешно пополнен. Ниже вы найдете детали операции:</p>
                            
                            <div class='amount-highlight'>
                                <div style='margin-bottom: 5px; font-size: 14px; color: rgba(255,255,255,0.8);'>Сумма пополнения:</div>
                                {topUp.Amount} ₽
                            </div>
                            
                            <div class='topup-details'>
                                <div class='detail-row'>
                                    <div class='detail-label'>ID операции:</div>
                                    <div class='detail-value'>{topUp.Id}</div>
                                </div>
                                
                                <div class='detail-row'>
                                    <div class='detail-label'>Дата:</div>
                                    <div class='detail-value'>{topUp.Date:dd.MM.yyyy HH:mm}</div>
                                </div>
                                
                                <div class='detail-row' style='border-bottom: none; margin-bottom: 0; padding-bottom: 0;'>
                                    <div class='detail-label'>Steam ID:</div>
                                    <div class='detail-value'>{topUp.SteamId}</div>
                                </div>
                            </div>
                            
                            <p>Средства уже зачислены на ваш аккаунт и доступны для использования.</p>
                            <p>Если у вас возникнут вопросы, не стесняйтесь обращаться в нашу службу поддержки.</p>
                        </div>
                        
                        <div class='footer'>
                            <p>© {DateTime.Now.Year} Game Store. Все права защищены.</p>
                            <p>Если вы не делали этого пополнения, пожалуйста, свяжитесь с нами.</p>
                        </div>
                    </div>
                </body>
                </html>
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