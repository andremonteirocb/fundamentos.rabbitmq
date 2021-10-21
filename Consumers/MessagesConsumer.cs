using Fundamentos.RabbitMQ.Models;
using Fundamentos.RabbitMQ.Options;
using Fundamentos.RabbitMQ.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fundamentos.RabbitMQ.Consumers
{
    public class MessagesConsumer : BackgroundService
    {
        private readonly RabbitMqConfiguration _config;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IServiceProvider _serviceProvider;
        public MessagesConsumer(
            IOptions<RabbitMqConfiguration> option,
            IServiceProvider serviceProvider)
        {
            _config = option.Value;
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory
            {
                HostName = _config.Host
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            #region Cria estrutura para DLQ
            _channel.ExchangeDeclare("DeadLetterExchange", ExchangeType.Fanout);
            _channel.QueueDeclare("DeadLetterQueue", true, false, false);
            _channel.QueueBind("DeadLetterQueue", "DeadLetterExchange", "");
            #endregion

            _channel.QueueDeclare(
                queue: _config.Queue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>()
                {
                    { "x-dead-letter-exchange", "DeadLetterExchange" }
                });
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (sender, eventArgs) =>
            {
                try
                {
                    var contentArray = eventArgs.Body.ToArray();
                    var contentString = Encoding.UTF8.GetString(contentArray);
                    var message = JsonConvert.DeserializeObject<InputModel>(contentString);
                    //var teste = Convert.ToInt16(message.Content);

                    NotifyUser(message);

                    _channel.BasicAck(eventArgs.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    _channel.BasicNack(eventArgs.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(_config.Queue, false, consumer);

            return Task.CompletedTask;
        }

        public void NotifyUser(InputModel message)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                notificationService.NotifyUser(message.ToId, message.Content);
            }
        }
    }
}
