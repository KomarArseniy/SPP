using System;
using System.Collections.Generic;

namespace SampleApp.Models
{
    public enum OrderStatus { Created, PendingPayment, Completed, Cancelled, Failed }

    public class OrderItem
    {
        public string Sku { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public User Customer { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public decimal TotalAmount { get; set; }
        public decimal DiscountApplied { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Created;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}