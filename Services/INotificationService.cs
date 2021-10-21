namespace Fundamentos.RabbitMQ.Services
{
    public interface INotificationService
    {
        void NotifyUser(int toId, string content);
    }
}
