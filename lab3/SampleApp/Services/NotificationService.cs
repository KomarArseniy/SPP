using System;
using SampleApp.Interfaces;

namespace SampleApp.Services
{
    public class NotificationService : INotificationService
    {
        public event Action<string> OnEmailSent;

        public void SendOrderConfirmation(string email, Guid orderId)
        {
            OnEmailSent?.Invoke($"Order {orderId} confirmed for {email}");
        }
    }
}