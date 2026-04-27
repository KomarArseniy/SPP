namespace SampleApp.Interfaces
{
    public interface INotificationService
    {
        void SendOrderConfirmation(string email, Guid orderId);
    }
}