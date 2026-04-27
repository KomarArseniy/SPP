using System.Threading.Tasks;

namespace SampleApp.Interfaces
{
    public interface IPaymentGateway
    {
        Task<bool> ChargeAsync(string userEmail, decimal amount);
    }
}