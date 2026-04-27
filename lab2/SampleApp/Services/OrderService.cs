using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SampleApp.Interfaces;
using SampleApp.Models;

namespace SampleApp.Services
{
    public class OrderService
    {
        private readonly IInventoryService _inventory;
        private readonly IPaymentGateway _payment;
        private readonly INotificationService _notification;

        public OrderService(IInventoryService inventory, IPaymentGateway payment, INotificationService notification)
        {
            _inventory = inventory;
            _payment = payment;
            _notification = notification;
        }

        public async Task<Order> CheckoutAsync(User user, List<OrderItem> items)
        {
            var order = new Order { Customer = user, Items = items, Status = OrderStatus.Created };
            decimal subTotal = 0;

            foreach (var item in items)
            {
                var product = _inventory.GetProduct(item.Sku);
                if (product == null) throw new KeyNotFoundException($"Product {item.Sku} not found");
                item.UnitPrice = product.Price;
                subTotal += product.Price * item.Quantity;
            }

            if (user.LoyaltyPoints > 100)
            {
                order.DiscountApplied = subTotal * 0.10m;
                user.LoyaltyPoints -= 10;
            }
            else
            {
                user.LoyaltyPoints += (int)(subTotal / 10);
            }
            order.TotalAmount = subTotal - order.DiscountApplied;

            var reservedItems = new List<OrderItem>();
            foreach (var item in items)
            {
                if (!_inventory.TryReserve(item.Sku, item.Quantity))
                {
                    foreach (var reserved in reservedItems) _inventory.Release(reserved.Sku, reserved.Quantity);
                    throw new InvalidOperationException($"Insufficient stock for {item.Sku}");
                }
                reservedItems.Add(item);
            }

            order.Status = OrderStatus.PendingPayment;

            try
            {
                bool paymentSuccess = await _payment.ChargeAsync(user.Email, order.TotalAmount);
                if (!paymentSuccess)
                {
                    await RollbackOrderAsync(items, user, subTotal, order);
                    order.Status = OrderStatus.Failed;
                    return order;
                }
            }
            catch (Exception)
            {
                await RollbackOrderAsync(items, user, subTotal, order);
                throw;
            }

            order.Status = OrderStatus.Completed;
            _notification.SendOrderConfirmation(user.Email, order.Id);

            return order;
        }

        private Task RollbackOrderAsync(List<OrderItem> items, User user, decimal subTotal, Order order)
        {
            foreach (var item in items) _inventory.Release(item.Sku, item.Quantity);

            if (order.DiscountApplied > 0) user.LoyaltyPoints += 10;
            else user.LoyaltyPoints -= (int)(subTotal / 10);

            return Task.CompletedTask;
        }
    }
}