namespace AccessoryWorld.Exceptions
{
    public class DomainException : Exception
    {
        public string ErrorCode { get; }
        
        public DomainException(string message) : base(message)
        {
            ErrorCode = "DOMAIN_ERROR";
        }
        
        public DomainException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
        
        public DomainException(string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = "DOMAIN_ERROR";
        }
        
        public DomainException(string message, string errorCode, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
    
    public static class DomainErrors
    {
        public const string INSUFFICIENT_STOCK = "INSUFFICIENT_STOCK";
        public const string PRODUCT_NOT_FOUND = "PRODUCT_NOT_FOUND";
        public const string PRODUCT_INACTIVE = "PRODUCT_INACTIVE";
        public const string INVALID_QUANTITY = "INVALID_QUANTITY";
        public const string CART_ITEM_NOT_FOUND = "CART_ITEM_NOT_FOUND";
        public const string ORDER_NOT_FOUND = "ORDER_NOT_FOUND";
        public const string INVALID_ORDER_STATE = "INVALID_ORDER_STATE";
        public const string PAYMENT_FAILED = "PAYMENT_FAILED";
        public const string INVALID_PAYMENT_AMOUNT = "INVALID_PAYMENT_AMOUNT";
        public const string CONCURRENCY_CONFLICT = "CONCURRENCY_CONFLICT";
        public const string SYSTEM_ERROR = "SYSTEM_ERROR";
    }
}