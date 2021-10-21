using Fundamentos.RabbitMQ.Models;
using Fundamentos.RabbitMQ.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fundamentos.RabbitMQ.Consumers
{
    public class TopicExchangeVitoriaConsumer : BackgroundService
    {
        private readonly string QUEUE_NAME = "topico-software-vitoria";
        private readonly string ROUTING_KEY = "*.vitoria";
        private readonly RabbitMqConfiguration _config;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        public TopicExchangeVitoriaConsumer(IOptions<RabbitMqConfiguration> option)
        {
            _config = option.Value;
            var factory = new ConnectionFactory
            {
                HostName = _config.Host
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_config.TopicExchange, ExchangeType.Topic, true);
            _channel.QueueDeclare(QUEUE_NAME, true, false, false, null);
            _channel.QueueBind(QUEUE_NAME, _config.TopicExchange, ROUTING_KEY);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (sender, eventArgs) =>
            {
                var contentArray = eventArgs.Body.ToArray();
                var contentString = Encoding.UTF8.GetString(contentArray);
                var message = JsonConvert.DeserializeObject<InputModel>(contentString);

                _channel.BasicAck(eventArgs.DeliveryTag, false);
            };

            _channel.BasicConsume(QUEUE_NAME, false, consumer);

            return Task.CompletedTask;
        }
    }
}
