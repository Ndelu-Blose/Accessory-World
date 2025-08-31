erDiagram
  User ||--o{ Address : has
  User ||--o{ Order : places
  Order ||--|{ OrderItem : contains
  Order ||--o| Payment : has
  Order ||--o| Shipment : has
  Order ||--o| PickupOTP : may_have
  Product ||--|{ SKU : variants
  SKU ||--o{ OrderItem : referenced_by
  SKU ||--o{ StockMovement : changes
  TradeInCase ||--o| CreditNote : issues
  User ||--o{ TradeInCase : creates
  User ||--o{ CreditNote : owns

  User {
    string id PK
    string name
    string email UNIQUE
    string phone UNIQUE
    string role
    datetime createdAt
  }

  Address {
    string id PK
    string userId FK
    string line1
    string city
    string postalCode
    string country
  }

  Product {
    string id PK
    string name
    string category
    string description
  }

  SKU {
    string id PK
    string productId FK
    string code UNIQUE
    string color
    string storage
    decimal priceZAR
    int stockOnHand
  }

  Order {
    string id PK
    string userId FK
    string status
    decimal totalZAR
    string deliveryMethod
    string addressId FK
    datetime createdAt
  }

  OrderItem {
    string id PK
    string orderId FK
    string skuId FK
    int qty
    decimal unitPriceZAR
  }

  Payment {
    string id PK
    string orderId FK
    string provider
    string txnId UNIQUE
    string status
    decimal amountZAR
    datetime createdAt
  }

  Shipment {
    string id PK
    string orderId FK
    string courier
    string trackingNumber
    string status
  }

  PickupOTP {
    string id PK
    string orderId FK
    string otp
    datetime expiresAt
    boolean used
  }

  TradeInCase {
    string id PK
    string userId FK
    string brand
    string model
    string imei UNIQUE
    string conditionGrade
    string status
  }

  CreditNote {
    string id PK
    string userId FK
    string code UNIQUE
    decimal valueZAR
    datetime expiresAt
    boolean consumed
  }

  StockMovement {
    string id PK
    string skuId FK
    int qtyDelta
    string reason
    string refId
    datetime createdAt
  }

  RMA {
    string id PK
    string orderId FK
    string status
    string reason
  }

  AuditLog {
    string id PK
    string actorId
    string action
    string entity
    string entityId
    string payload
    datetime createdAt
  }
