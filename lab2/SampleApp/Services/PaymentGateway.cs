using SampleApp.Exceptions;
using SampleApp.Interfaces;
using System;
using System.Threading.Tasks;

namespace SampleApp.Services
{
    public class PaymentGateway : IPaymentGateway
    {
        public async Task<bool> ChargeAsync(string userEmail, decimal amount)
        {
            await Task.Delay(50);

            if (amount <= 0) throw new ArgumentException("Amount must be positive");

            if (amount > 5000)
            {
                throw new PaymentFailedException("Bank limit exceeded");
            }

            if (amount > 1000)
            {
                return false;
            }

            return true;
        }
    }
}