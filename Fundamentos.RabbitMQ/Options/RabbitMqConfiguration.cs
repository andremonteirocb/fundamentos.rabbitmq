namespace Fundamentos.RabbitMQ.Options
{
    public class RabbitMqConfiguration
    {
        public string Host { get; set; }
        public string Queue { get; set; }
        public string FanoutExchange { get; set; }
        public string DirectExchange { get; set; }
        public string TopicExchange { get; set; }
        //public string Username { get; set; }
        //public string Password { get; set; }
    }
}
