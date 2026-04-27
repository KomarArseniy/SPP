using SampleApp.Exceptions;
using SampleApp.Interfaces;
using SampleApp.Models;
using System.Collections.Concurrent;
using SampleApp.Exceptions;

namespace SampleApp.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ConcurrentDictionary<string, Product> _inventory = new ConcurrentDictionary<string, Product>();

        public void AddStock(string sku, string name, decimal price, int quantity)
        {
            _inventory[sku] = new Product { Sku = sku, Name = name, Price = price, StockQuantity = quantity };
        }

        public Product GetProduct(string sku)
        {
            return _inventory.TryGetValue(sku, out var product) ? product : null;
        }

        public bool TryReserve(string sku, int quantity)
        {
            if (!_inventory.TryGetValue(sku, out var product)) return false;

            lock (product) 
            {
                if (product.StockQuantity < quantity) return false;
                product.StockQuantity -= quantity;
                return true;
            }
        }

        public void Release(string sku, int quantity)
        {
            if (_inventory.TryGetValue(sku, out var product))
            {
                lock (product)
                {
                    product.StockQuantity += quantity;
                }
            }
        }
        public void ReserveOrThrow(string sku, int quantity)
        {
            if (!_inventory.TryGetValue(sku, out var product))
                throw new KeyNotFoundException($"Product {sku} not found");

            lock (product)
            {
                if (product.StockQuantity < quantity)
                {
                    throw new InsufficientStockException(sku, product.StockQuantity);
                }
                product.StockQuantity -= quantity;
            }
        }
    }
}