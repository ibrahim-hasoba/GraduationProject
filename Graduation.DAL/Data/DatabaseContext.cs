using Graduation.DAL.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Graduation.DAL.Data
{
    public class DatabaseContext : IdentityDbContext<AppUser>
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Vendor Configuration
            builder.Entity<Vendor>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.HasOne(v => v.User)
                    .WithMany()
                    .HasForeignKey(v => v.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(v => v.StoreName).IsUnique();
                entity.Property(v => v.StoreName).IsRequired().HasMaxLength(200);
                entity.Property(v => v.PhoneNumber).IsRequired().HasMaxLength(20);
            });

            // Category Configuration
            builder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasOne(c => c.ParentCategory)
                    .WithMany(c => c.SubCategories)
                    .HasForeignKey(c => c.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(c => c.NameAr).IsRequired().HasMaxLength(200);
                entity.Property(c => c.NameEn).IsRequired().HasMaxLength(200);
            });

            // Product Configuration
            builder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Price).HasPrecision(18, 2);
                entity.Property(p => p.DiscountPrice).HasPrecision(18, 2);
                entity.Property(p => p.NameAr).IsRequired().HasMaxLength(300);
                entity.Property(p => p.NameEn).IsRequired().HasMaxLength(300);
                entity.Property(p => p.SKU).IsRequired().HasMaxLength(100);

                entity.HasOne(p => p.Vendor)
                    .WithMany(v => v.Products)
                    .HasForeignKey(p => p.VendorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => p.SKU).IsUnique();
            });

            // Product Image Configuration
            builder.Entity<ProductImage>(entity =>
            {
                entity.HasKey(pi => pi.Id);

                entity.HasOne(pi => pi.Product)
                    .WithMany(p => p.Images)
                    .HasForeignKey(pi => pi.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(pi => pi.ImageUrl).IsRequired();
            });

            // Product Review Configuration
            builder.Entity<ProductReview>(entity =>
            {
                entity.HasKey(pr => pr.Id);

                entity.HasOne(pr => pr.Product)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(pr => pr.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pr => pr.User)
                    .WithMany()
                    .HasForeignKey(pr => pr.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(pr => pr.Rating).IsRequired();
            });

            // Cart Item Configuration
            builder.Entity<CartItem>(entity =>
            {
                entity.HasKey(ci => ci.Id);

                entity.HasOne(ci => ci.User)
                    .WithMany()
                    .HasForeignKey(ci => ci.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.Product)
                    .WithMany(p => p.CartItems)
                    .HasForeignKey(ci => ci.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(ci => new { ci.UserId, ci.ProductId }).IsUnique();
            });

            // Order Configuration
            builder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.Property(o => o.SubTotal).HasPrecision(18, 2);
                entity.Property(o => o.ShippingCost).HasPrecision(18, 2);
                entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
                entity.Property(o => o.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(o => o.ShippingPhone).IsRequired().HasMaxLength(20);

                entity.HasOne(o => o.User)
                    .WithMany()
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Vendor)
                    .WithMany(v => v.Orders)
                    .HasForeignKey(o => o.VendorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(o => o.OrderNumber).IsUnique();
            });

            // Order Item Configuration
            builder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(oi => oi.Id);

                entity.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
                entity.Property(oi => oi.TotalPrice).HasPrecision(18, 2);

                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);

                entity.HasOne(rt => rt.User)
                    .WithMany()
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(rt => rt.Token).IsUnique();
                entity.HasIndex(rt => rt.UserId);
                entity.Property(rt => rt.Token).IsRequired().HasMaxLength(200);
            });

            // Seed Egyptian Categories
            SeedEgyptianCategories(builder);
        }

        private void SeedEgyptianCategories(ModelBuilder builder)
        {
            builder.Entity<Category>().HasData(
                new Category { Id = 1, NameAr = "منتجات غذائية", NameEn = "Food Products", Description = "Traditional Egyptian food products" },
                new Category { Id = 2, NameAr = "الحرف اليدوية", NameEn = "Handicrafts", Description = "Egyptian handicrafts and traditional arts" },
                new Category { Id = 3, NameAr = "المنسوجات", NameEn = "Textiles", Description = "Egyptian cotton and traditional fabrics" },
                new Category { Id = 4, NameAr = "المجوهرات", NameEn = "Jewelry", Description = "Egyptian jewelry and accessories" },
                new Category { Id = 5, NameAr = "الأثاث والديكور", NameEn = "Furniture & Decor", Description = "Egyptian furniture and home decor" },
                new Category { Id = 6, NameAr = "العطور والزيوت", NameEn = "Perfumes & Oils", Description = "Egyptian essential oils and perfumes" },
                new Category { Id = 7, NameAr = "السجاد والكليم", NameEn = "Carpets & Rugs", Description = "Egyptian carpets and traditional rugs" },
                new Category { Id = 8, NameAr = "البردي والورق", NameEn = "Papyrus & Paper", Description = "Papyrus art and handmade paper" },
                new Category { Id = 9, NameAr = "الفخار والخزف", NameEn = "Pottery & Ceramics", Description = "Egyptian pottery and ceramics" },
                new Category { Id = 10, NameAr = "النحاس والمعادن", NameEn = "Copper & Metals", Description = "Copper crafts and metalwork" }
            );
        }
    }
}