using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Models;

namespace AccessoryWorld.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        // DbSets for all entities
        public DbSet<Address> Addresses { get; set; } = null!;
        public DbSet<Brand> Brands { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<SKU> SKUs { get; set; } = null!;
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<ProductSpecification> ProductSpecifications { get; set; } = null!;
        public DbSet<StockMovement> StockMovements { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Shipment> Shipments { get; set; } = null!;
        public DbSet<PickupOTP> PickupOTPs { get; set; } = null!;
        public DbSet<RMA> RMAs { get; set; } = null!;
        public DbSet<RMAItem> RMAItems { get; set; } = null!;
        public DbSet<TradeIn> TradeIns { get; set; } = null!;
        public DbSet<CreditNote> CreditNotes { get; set; } = null!;
        public DbSet<StockItem> StockItems { get; set; } = null!;
        public DbSet<TradeInCase> TradeInCases { get; set; } = null!;
        public DbSet<TradeInImage> TradeInImages { get; set; } = null!;
        public DbSet<TradeInEvaluation> TradeInEvaluations { get; set; } = null!;
        public DbSet<DeviceModel> DeviceModels { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<Role> CustomRoles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<UserRole> CustomUserRoles { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Newsletter> Newsletters { get; set; } = null!;
        public DbSet<Settings> Settings { get; set; } = null!;
        public DbSet<Wishlist> Wishlists { get; set; } = null!;
        public DbSet<CheckoutSession> CheckoutSessions { get; set; } = null!;
        public DbSet<StockLock> StockLocks { get; set; } = null!;
        public DbSet<CreditNoteLock> CreditNoteLocks { get; set; } = null!;
        public DbSet<WebhookEvent> WebhookEvents { get; set; } = null!;
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure relationships and constraints
            
            // User -> Address (One-to-Many)
            modelBuilder.Entity<Address>()
                .HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Address entity configuration
            modelBuilder.Entity<Address>()
                .HasKey(a => a.Id);
            
            modelBuilder.Entity<Address>()
                .HasIndex(a => a.PublicId)
                .IsUnique();
            
            modelBuilder.Entity<Address>()
                .Property(a => a.PublicId)
                .IsRequired()
                .HasDefaultValueSql("NEWID()");
            
            // Ensure single default per user (SQL Server filtered index)
            modelBuilder.Entity<Address>()
                .HasIndex(a => new { a.UserId, a.IsDefault })
                .HasFilter("[IsDefault] = 1")
                .IsUnique();
            
            // Product -> SKU (One-to-Many)
            modelBuilder.Entity<SKU>()
                .HasOne(s => s.Product)
                .WithMany(p => p.SKUs)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Brand -> Product (One-to-Many)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Category -> Product (One-to-Many)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Product -> ProductImage (One-to-Many)
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Product -> ProductSpecification (One-to-Many)
            modelBuilder.Entity<ProductSpecification>()
                .HasOne(ps => ps.Product)
                .WithMany(p => p.ProductSpecifications)
                .HasForeignKey(ps => ps.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            

            
            // User -> StockMovement (One-to-Many)
            modelBuilder.Entity<StockMovement>()
                .HasOne(sm => sm.User)
                .WithMany()
                .HasForeignKey(sm => sm.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // User -> Order (One-to-Many)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Order-Address relationship - using int Id for now
            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShippingAddress)
                .WithMany(a => a.Orders)
                .HasForeignKey(o => o.ShippingAddressId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Order -> OrderItem (One-to-Many)
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // SKU -> OrderItem (One-to-Many)
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.SKU)
                .WithMany(s => s.OrderItems)
                .HasForeignKey(oi => oi.SKUId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Order -> Payment (One-to-Many)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Order -> Shipment (One-to-One)
            modelBuilder.Entity<Shipment>()
                .HasOne(s => s.Order)
                .WithOne(o => o.Shipment)
                .HasForeignKey<Shipment>(s => s.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Order -> PickupOTP (One-to-One)
            modelBuilder.Entity<PickupOTP>()
                .HasOne(p => p.Order)
                .WithOne(o => o.PickupOTP)
                .HasForeignKey<PickupOTP>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // User -> TradeInCase (One-to-Many)
            modelBuilder.Entity<TradeInCase>()
                .HasOne(t => t.User)
                .WithMany(u => u.TradeInCases)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // TradeInCase -> TradeInImage (One-to-Many)
            modelBuilder.Entity<TradeInImage>()
                .HasOne(ti => ti.TradeInCase)
                .WithMany(t => t.Images)
                .HasForeignKey(ti => ti.TradeInCaseId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // TradeInCase -> TradeInEvaluation (One-to-One)
            modelBuilder.Entity<TradeInEvaluation>()
                .HasOne(te => te.TradeInCase)
                .WithOne(t => t.Evaluation)
                .HasForeignKey<TradeInEvaluation>(te => te.TradeInCaseId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // User -> TradeInEvaluation (One-to-Many)
            modelBuilder.Entity<TradeInEvaluation>()
                .HasOne(te => te.EvaluatedByUser)
                .WithMany()
                .HasForeignKey(te => te.EvaluatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // User -> CreditNote (One-to-Many)
            modelBuilder.Entity<CreditNote>()
                .HasOne(cn => cn.User)
                .WithMany(u => u.CreditNotes)
                .HasForeignKey(cn => cn.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // TradeInCase -> CreditNote (One-to-Many)
            modelBuilder.Entity<CreditNote>()
                .HasOne(cn => cn.TradeInCase)
                .WithMany()
                .HasForeignKey(cn => cn.TradeInCaseId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Order -> CreditNote (One-to-Many for consumed orders)
            modelBuilder.Entity<CreditNote>()
                .HasOne(cn => cn.ConsumedInOrder)
                .WithMany()
                .HasForeignKey(cn => cn.ConsumedInOrderId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // User -> RMA (One-to-Many)
            modelBuilder.Entity<RMA>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Order -> RMA (One-to-Many)
            modelBuilder.Entity<RMA>()
                .HasOne(r => r.Order)
                .WithMany(o => o.RMAs)
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // RMA -> RMAItem (One-to-Many)
            modelBuilder.Entity<RMAItem>()
                .HasOne(ri => ri.RMA)
                .WithMany(r => r.RMAItems)
                .HasForeignKey(ri => ri.RMAId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // OrderItem -> RMAItem (One-to-Many)
            modelBuilder.Entity<RMAItem>()
                .HasOne(ri => ri.OrderItem)
                .WithMany()
                .HasForeignKey(ri => ri.OrderItemId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // User -> AuditLog (One-to-Many)
            modelBuilder.Entity<AuditLog>()
                .Property(a => a.UserId)
                .HasMaxLength(450); // Match AspNetUsers.Id length
                
            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            
            // RBAC relationships
            modelBuilder.Entity<UserRole>()
                .ToTable("CustomUserRoles")
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.AssignedByUser)
                .WithMany()
                .HasForeignKey(ur => ur.AssignedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
            
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes for performance and uniqueness
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();
            
            modelBuilder.Entity<SKU>()
                .HasIndex(s => s.SKUCode)
                .IsUnique();
            
            modelBuilder.Entity<TradeInCase>()
                .HasIndex(t => t.CaseNumber)
                .IsUnique();
            
            modelBuilder.Entity<TradeInCase>()
                .HasIndex(t => t.IMEI);
            
            modelBuilder.Entity<CreditNote>()
                .HasIndex(cn => cn.CreditNoteCode)
                .IsUnique();
            
            modelBuilder.Entity<RMA>()
                .HasIndex(r => r.RMANumber)
                .IsUnique();
            
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.PaymentIntentId);
            
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.TransactionId);
            
            // Additional unique constraints for data integrity
            modelBuilder.Entity<Brand>()
                .HasIndex(b => b.Name)
                .IsUnique();
            
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();
            
            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();
            
            // Address indexes for performance and data integrity
            modelBuilder.Entity<Address>()
                .HasIndex(a => a.UserId);
            
            modelBuilder.Entity<Address>()
                .HasIndex(a => new { a.UserId, a.IsDefault });
            
            modelBuilder.Entity<Address>()
                .HasIndex(a => a.PublicId).IsUnique();
            
            // Check constraints for business rules
            modelBuilder.Entity<Product>()
                .ToTable(t => {
                    t.HasCheckConstraint("CK_Product_Price_Positive", "[Price] >= 0");
                    t.HasCheckConstraint("CK_Product_SalePrice_Valid", "[SalePrice] IS NULL OR ([SalePrice] >= 0 AND [SalePrice] < [Price])");
                });
            
            modelBuilder.Entity<SKU>()
                .ToTable(t => {
                    t.HasCheckConstraint("CK_SKU_StockQuantity_NonNegative", "[StockQuantity] >= 0");
                    t.HasCheckConstraint("CK_SKU_ReservedQuantity_Valid", "[ReservedQuantity] >= 0 AND [ReservedQuantity] <= [StockQuantity]");
                    t.HasCheckConstraint("CK_SKU_LowStockThreshold_NonNegative", "[LowStockThreshold] >= 0");
                    t.HasCheckConstraint("CK_SKU_Price_Positive", "[Price] >= 0");
                });
            
            modelBuilder.Entity<Order>()
                .ToTable(t => {
                    t.HasCheckConstraint("CK_Order_Subtotal_Positive", "[Subtotal] >= 0");
                    t.HasCheckConstraint("CK_Order_VATAmount_NonNegative", "[VATAmount] >= 0");
                    t.HasCheckConstraint("CK_Order_ShippingFee_NonNegative", "[ShippingFee] >= 0");
                    t.HasCheckConstraint("CK_Order_Total_Positive", "[Total] > 0");
                    t.HasCheckConstraint("CK_Order_DiscountAmount_Valid", "[DiscountAmount] >= 0 AND [DiscountAmount] <= [Subtotal]");
                    t.HasCheckConstraint("CK_Order_CreditNoteAmount_NonNegative", "[CreditNoteAmount] >= 0");
                });
            
            modelBuilder.Entity<OrderItem>()
                .ToTable(t => {
                    t.HasCheckConstraint("CK_OrderItem_Quantity_Positive", "[Quantity] > 0");
                    t.HasCheckConstraint("CK_OrderItem_UnitPrice_NonNegative", "[UnitPrice] >= 0");
                });
            
            modelBuilder.Entity<CartItem>()
                .ToTable(t => {
                    t.HasCheckConstraint("CK_CartItem_Quantity_Positive", "[Quantity] > 0 AND [Quantity] <= 100");
                    t.HasCheckConstraint("CK_CartItem_UnitPrice_NonNegative", "[UnitPrice] >= 0");
                });
            
            modelBuilder.Entity<Payment>()
                .ToTable(t => {
                    t.HasCheckConstraint("CK_Payment_Amount_Positive", "[Amount] > 0");
                });
            
            modelBuilder.Entity<TradeInEvaluation>()
                .ToTable(t => {
                    t.HasCheckConstraint("CK_TradeInEvaluation_BaseValue_NonNegative", "[BaseValue] >= 0");
                    t.HasCheckConstraint("CK_TradeInEvaluation_FinalOfferAmount_NonNegative", "[FinalOfferAmount] >= 0");
                    t.HasCheckConstraint("CK_TradeInEvaluation_AccessoryBonus_NonNegative", "[AccessoryBonus] >= 0");
                });
            
            modelBuilder.Entity<CreditNote>()
                .ToTable(t => {
                    t.HasCheckConstraint("CK_CreditNote_Amount_Positive", "[Amount] > 0");
                });
            
            modelBuilder.Entity<DeviceModel>()
                .ToTable(t => {
                    t.HasCheckConstraint("CK_DeviceModel_BaseTradeInValue_NonNegative", "[BaseTradeInValue] >= 0");
                });
            
            // CartItem -> Product (Many-to-One)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
            
            // CartItem -> SKU (Many-to-One)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.SKU)
                .WithMany()
                .HasForeignKey(ci => ci.SKUId)
                .OnDelete(DeleteBehavior.NoAction);
            
            // CartItem -> User (Many-to-One, optional)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.User)
                .WithMany()
                .HasForeignKey(ci => ci.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Index for efficient cart queries
            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => ci.SessionId);
            
            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => ci.UserId);
            
            // Configure TradeIn
            modelBuilder.Entity<TradeIn>(entity =>
            {
                entity.ToTable("TradeIns"); // Explicit table mapping to match the database table
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.CustomerId).IsRequired();
                entity.Property(e => e.DeviceBrand).IsRequired().HasMaxLength(64).HasDefaultValue("Apple");
                entity.Property(e => e.DeviceModel).IsRequired().HasMaxLength(128);
                entity.Property(e => e.IMEI).HasMaxLength(32);
                entity.Property(e => e.ConditionGrade).IsRequired().HasMaxLength(2);
                entity.Property(e => e.PhotosJson).IsRequired().HasColumnType("nvarchar(max)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
                entity.Property(e => e.ProposedValue).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ApprovedValue).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasColumnType("nvarchar(max)");
                entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.RowVersion).IsRowVersion();
                
                // Indexes
                entity.HasIndex(e => e.PublicId).IsUnique();
                entity.HasIndex(e => new { e.CustomerId, e.Status });
                
                // Check constraint for ConditionGrade
                entity.ToTable(t => t.HasCheckConstraint("CK_TradeIn_ConditionGrade", "[ConditionGrade] IN ('A','B','C','D')"));
                
                // Relationships
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.Property(e => e.ApprovedBy)
                    .HasMaxLength(450);
                    
                entity.HasOne(e => e.ApprovedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            // ===============================
            // REPLACEMENT: CreditNote / TradeInCase / User / TradeIn wiring
            // ===============================
            modelBuilder.Entity<CreditNote>(entity =>
            {
                // Keys & columns
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CreditNoteCode)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(e => e.UserId)
                      .IsRequired()
                      .HasMaxLength(450);

                entity.Property(e => e.TradeInId)
                      .IsRequired(); // FK to TradeIn (1:1)

                entity.Property(e => e.TradeInCaseId);     // nullable
                entity.Property(e => e.ConsumedInOrderId); // nullable

                entity.Property(e => e.Amount)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.AmountRemaining)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Status)
                      .IsRequired()
                      .HasMaxLength(32);

                entity.Property(e => e.ExpiresAt)
                      .IsRequired();

                entity.Property(e => e.CreatedAt)
                      .IsRequired()
                      .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.Property(e => e.RedeemedAt);     // nullable
                entity.Property(e => e.RedeemedOrderId);// nullable

                entity.Property(e => e.RowVersion).IsRowVersion();

                // Indexes
                entity.HasIndex(e => e.CreditNoteCode).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.Status });
                // One CreditNote per TradeIn
                entity.HasIndex(e => e.TradeInId).IsUnique();

                // Relationships

                // Bind to ApplicationUser via UserId (prevents shadow 'ApplicationUserId')
                entity.HasOne(e => e.User)
                      .WithMany(u => u.CreditNotes)              // <-- bind to the collection on ApplicationUser
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Bind to TradeInCase via TradeInCaseId (prevents shadow 'TradeInCaseId1')
                entity.HasOne(e => e.TradeInCase)
                      .WithMany(tc => tc.CreditNotes)            // <-- bind to the collection on TradeInCase
                      .HasForeignKey(e => e.TradeInCaseId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Optional: the order in which a credit note was consumed
                entity.HasOne(e => e.ConsumedInOrder)
                      .WithMany()                                // no back-collection on Order
                      .HasForeignKey(e => e.ConsumedInOrderId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Ensure the 1:1 between TradeIn and CreditNote is explicit
            modelBuilder.Entity<TradeIn>(entity =>
            {
                entity.HasOne(t => t.CreditNote)
                      .WithOne()                                 // CreditNote has no nav back to TradeIn
                      .HasForeignKey<CreditNote>(cn => cn.TradeInId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            
            // Configure StockItem
            modelBuilder.Entity<StockItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SKUId).IsRequired();
                entity.Property(e => e.IsTradeInUnit).IsRequired().HasDefaultValue(false);
                
                // Index
                entity.HasIndex(e => e.SourceTradeInId);
                
                // Relationships
                entity.HasOne(e => e.SKU)
                    .WithMany()
                    .HasForeignKey(e => e.SKUId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.SourceTradeIn)
                    .WithMany(t => t.StockItems)
                    .HasForeignKey(e => e.SourceTradeInId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Wishlist
            modelBuilder.Entity<Wishlist>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                
                // Create composite unique index to prevent duplicate wishlist items
                entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
                
                // Configure relationships
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // StockMovement configuration
            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MovementType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ReasonCode).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                // Relationships
                entity.HasOne(e => e.SKU)
                    .WithMany()
                    .HasForeignKey(e => e.SKUId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.TradeIn)
                    .WithMany(t => t.StockMovements)
                    .HasForeignKey(e => e.TradeInId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasOne(e => e.CreditNote)
                    .WithMany(c => c.StockMovements)
                    .HasForeignKey(e => e.CreditNoteId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                // Indexes
                entity.HasIndex(e => e.SKUId);
                entity.HasIndex(e => e.MovementType);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.TradeInId);
                entity.HasIndex(e => e.CreditNoteId);
            });

            // CheckoutSession configuration
            modelBuilder.Entity<CheckoutSession>(entity =>
            {
                entity.HasKey(e => e.SessionId);
                entity.Property(e => e.SessionId).IsRequired();
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.AppliedCreditNoteCode).HasMaxLength(20);
                entity.Property(e => e.CreditNoteAmount).HasColumnType("decimal(18,2)");
                
                entity.HasIndex(e => e.SessionId).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ExpiresAt);
            });

            // StockLock configuration
            modelBuilder.Entity<StockLock>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                
                entity.HasOne(e => e.SKU)
                    .WithMany()
                    .HasForeignKey(e => e.SKUId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.SKUId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ExpiresAt);
            });

            // CreditNoteLock configuration
            modelBuilder.Entity<CreditNoteLock>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionId).IsRequired();
                entity.Property(e => e.CreditNoteCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.LockedAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                
                entity.HasOne(e => e.CheckoutSession)
                    .WithMany(cs => cs.CreditNoteLocks)
                    .HasForeignKey(e => e.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.CreditNote)
                    .WithMany()
                    .HasForeignKey(e => e.CreditNoteCode)
                    .HasPrincipalKey(cn => cn.CreditNoteCode)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.CreditNoteCode);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ExpiresAt);
            });

            // WebhookEvent configuration
            modelBuilder.Entity<WebhookEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Source).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Payload).HasColumnType("nvarchar(max)");
                entity.Property(e => e.ProcessingResult).HasColumnType("nvarchar(max)");
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
                entity.Property(e => e.ReceivedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
                
                // Relationships
                entity.HasOne(e => e.RelatedOrder)
                    .WithMany()
                    .HasForeignKey(e => e.RelatedOrderId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasOne(e => e.RelatedTradeInCase)
                    .WithMany()
                    .HasForeignKey(e => e.RelatedTradeInCaseId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasOne(e => e.RelatedCreditNote)
                    .WithMany()
                    .HasForeignKey(e => e.RelatedCreditNoteId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Indexes
                entity.HasIndex(e => e.EventId).IsUnique();
                entity.HasIndex(e => new { e.EventType, e.Source });
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ReceivedAt);
                entity.HasIndex(e => e.RelatedOrderId);
                entity.HasIndex(e => e.RelatedTradeInCaseId);
                entity.HasIndex(e => e.RelatedCreditNoteId);
            });

            // Settings configuration - Add decimal precision to silence warnings
            modelBuilder.Entity<Settings>(entity =>
            {
                entity.Property(e => e.ShippingCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxRate).HasColumnType("decimal(18,4)");
            });
        }
    }
}