using Fundamentos.RabbitMQ.Models;
using Fundamentos.RabbitMQ.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fundamentos.RabbitMQ.Controllers
{
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly ConnectionFactory _factory;
        private readonly RabbitMqConfiguration _config;
        public MessagesController(IOptions<RabbitMqConfiguration> options)
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
            var json = JsonConvert.SerializeObject(message);
            Enviar(json, _config.Queue);

            return Accepted();
        }

        private void Channel_BasicReturn(object sender, BasicReturnEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Body.ToArray());
            Console.WriteLine($"{DateTime.UtcNow:o} Basic Return => {message}");

            //Enviar(message, _config.Queue);
        }

        private void Channel_BasicAcks(object sender, BasicAckEventArgs e)
        {
            Console.WriteLine($"{DateTime.UtcNow:o} Basic Acks");
        }

        private void Channel_BasicNacks(object sender, BasicNackEventArgs e)
        {
            Console.WriteLine($"{DateTime.UtcNow:o} Basic Nacks");
        }

        private void Enviar(string message, string routingKey)
        {
            using (var connection = _factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    #region prepara a estrutura de DLQ

                    channel.ExchangeDeclare("DeadLetterExchange", ExchangeType.Fanout);
                    channel.QueueDeclare("DeadLetterQueue", true, false, false);
                    channel.QueueBind("DeadLetterQueue", "DeadLetterExchange", "");

                    var arguments = new Dictionary<string, object>()
                    {
                        { "x-dead-letter-exchange", "DeadLetterExchange" }
                    };

                    #endregion

                    #region Confirm and Return Message

                    //ativa a confirmação de mensagem
                    channel.ConfirmSelect();

                    //mensagem recebida no broken
                    channel.BasicAcks += Channel_BasicAcks;
                    //mensagem não recebida pelo broken
                    channel.BasicNacks += Channel_BasicNacks;
                    //retorna a mensagem em caso de erro
                    //lembrar de passar o mandatory: true, no basicpublish
                    channel.BasicReturn += Channel_BasicReturn;

                    #endregion

                    channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                    channel.QueueDeclare(
                        queue: _config.Queue,
                        durable: false,
                        exclusive: false,
                        autoDelete: false,
                        arguments: arguments);

                    var bytesMessage = Encoding.UTF8.GetBytes(message);
                    channel.BasicPublish(
                        exchange: "",
                        routingKey: routingKey,
                        basicProperties: null,
                        body: bytesMessage);

                    //aguarda 5 seconds até receber a confirmação
                    channel.WaitForConfirms(new TimeSpan(0, 0, 5));
                }
            }
        }
    }
}
