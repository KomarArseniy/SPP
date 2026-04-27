using System;

namespace SampleApp.Exceptions
{
    public class OrderProcessingException : Exception
    {
        public OrderProcessingException(string message) : base(message) { }
    }

    public class InsufficientStockException : OrderProcessingException
    {
        public string Sku { get; }
        public int Available { get; }

        public InsufficientStockException(string sku, int available)
            : base($"Insufficient stock for product {sku}. Available: {available}")
        {
            Sku = sku;
            Available = available;
        }
    }

    public class PaymentFailedException : OrderProcessingException
    {
        public string Reason { get; }

        public PaymentFailedException(string reason)
            : base($"Payment failed: {reason}")
        {
            Reason = reason;
        }
    }
}