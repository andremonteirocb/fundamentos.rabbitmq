using Fundamentos.RabbitMQ.Models;
using Fundamentos.RabbitMQ.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace Fundamentos.RabbitMQ.Controllers
{
    [Route("api/[controller]")]
    public class TopicController : ControllerBase
    {
        private readonly ConnectionFactory _factory;
        private readonly RabbitMqConfiguration _config;
        public TopicController(IOptions<RabbitMqConfiguration> options)
        {
            _config = options.Value;
            _factory = new ConnectionFactory
            {
                HostName = _config.Host
            };
        }

        [HttpPost]
        public IActionResult Post([FromBody] InputModel message)
        {
            using (var connection = _factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    var stringfiedMessage = JsonConvert.SerializeObject(message);
                    var bytesMessage = Encoding.UTF8.GetBytes(stringfiedMessage);

                    channel.BasicPublish(
                        exchange: _config.TopicExchange,
                        routingKey: message.Content,
                        basicProperties: null,
                        body: bytesMessage);
                }
            }

            return Accepted();
        }
    }
}
