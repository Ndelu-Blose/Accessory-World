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
        public DbSet<TradeInCase> TradeInCases { get; set; } = null!;
        public DbSet<TradeInImage> TradeInImages { get; set; } = null!;
        public DbSet<TradeInEvaluation> TradeInEvaluations { get; set; } = null!;
        public DbSet<CreditNote> CreditNotes { get; set; } = null!;
        public DbSet<DeviceModel> DeviceModels { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<Role> CustomRoles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<UserRole> CustomUserRoles { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Newsletter> Newsletters { get; set; }
        public DbSet<Settings> Settings { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; } = null!;
        
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
            
            // SKU -> StockMovement (One-to-Many)
            modelBuilder.Entity<StockMovement>()
                .HasOne(sm => sm.SKU)
                .WithMany(s => s.StockMovements)
                .HasForeignKey(sm => sm.SKUId)
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
            
            // Address -> Order (One-to-Many)
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
            
            // TradeInCase -> CreditNote (One-to-One)
            modelBuilder.Entity<CreditNote>()
                .HasOne(cn => cn.TradeInCase)
                .WithOne(t => t.CreditNote)
                .HasForeignKey<CreditNote>(cn => cn.TradeInCaseId)
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
        }
    }
}