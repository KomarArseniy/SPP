using SampleApp.Models;

namespace SampleApp.Interfaces
{
    public interface IInventoryService
    {
        Product GetProduct(string sku);
        bool TryReserve(string sku, int quantity);
        void Release(string sku, int quantity);
    }
}