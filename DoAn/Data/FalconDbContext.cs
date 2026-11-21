
using Microsoft.EntityFrameworkCore;
using Falcon.Web.Models;
using Falcon.Web.Models.Admin;

namespace Falcon.Web.Data;

public class FalconDbContext : DbContext
{
    public FalconDbContext(
        DbContextOptions<FalconDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<SalesOrder> Orders => Set<SalesOrder>();
    public DbSet<TestDriveBooking> TestDriveBookings => Set<TestDriveBooking>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Showroom> Showrooms => Set<Showroom>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<CustomerProfile>().HasIndex(x => x.UserId).IsUnique();
        modelBuilder.Entity<CustomerProfile>().HasOne<AppUser>().WithOne().HasForeignKey<CustomerProfile>(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Coupon>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<SalesOrder>().HasIndex(x => x.OrderCode).IsUnique();
        modelBuilder.Entity<SalesOrder>().HasOne(x => x.Coupon).WithMany().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SalesOrder>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SalesOrder>().HasMany(x => x.Details).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OrderDetail>().HasOne<Product>().WithMany().HasForeignKey(x => x.CarId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<OrderDetail>().Property(x => x.TotalPrice).HasComputedColumnSql("[Quantity] * [UnitPrice]", stored: true).HasPrecision(29, 2);
        modelBuilder.Entity<TestDriveBooking>().HasIndex(x => x.BookingCode).IsUnique();
        modelBuilder.Entity<TestDriveBooking>().HasOne(x => x.Car).WithMany().HasForeignKey(x => x.CarId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TestDriveBooking>().HasOne(x => x.Showroom).WithMany().HasForeignKey(x => x.ShowroomId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TestDriveBooking>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);

        // Bảng Categories
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories", "dbo");
            entity.HasKey(x => x.CategoryId);

            entity.Property(x => x.CategoryId)
                .HasColumnName("category_id");

            entity.Property(x => x.CategoryName)
                .HasColumnName("category_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Slug)
                .HasColumnName("slug")
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasColumnName("description");

            entity.Property(x => x.IsActive)
                .HasColumnName("is_active");
        });

        // Bảng Products
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products", "dbo");
            entity.HasKey(x => x.ProductId);

            entity.Property(x => x.ProductId)
                .HasColumnName("product_id");

            entity.Property(x => x.CategoryId)
                .HasColumnName("category_id");

            entity.Property(x => x.ProductName)
                .HasColumnName("product_name")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Slug)
                .HasColumnName("slug")
                .HasMaxLength(150)
                .IsUnicode(false)
                .IsRequired();

            entity.HasIndex(x => x.Slug).IsUnique();

            entity.Property(x => x.Price)
                .HasColumnName("price")
                .HasPrecision(15, 2);

            entity.Property(x => x.Displacement)
                .HasColumnName("displacement");

            entity.Property(x => x.EngineType)
                .HasColumnName("engine_type")
                .HasMaxLength(100);

            entity.Property(x => x.FuelCapacity)
                .HasColumnName("fuel_capacity")
                .HasPrecision(4, 2);

            entity.Property(x => x.ColorOptions)
                .HasColumnName("color_options")
                .HasMaxLength(255);

            entity.Property(x => x.StockQuantity)
                .HasColumnName("stock_quantity").IsConcurrencyToken();

            entity.Property(x => x.Description)
                .HasColumnName("description");

            entity.Property(x => x.IsFeatured)
                .HasColumnName("is_featured");

            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsUnicode(true)
                .HasDefaultValue("Active");

            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Bảng ProductImages
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImages", "dbo");
            entity.HasKey(x => x.ImageId);

            entity.Property(x => x.ImageId)
                .HasColumnName("image_id");

            entity.Property(x => x.ProductId)
                .HasColumnName("product_id");

            entity.Property(x => x.ImageUrl)
                .HasColumnName("image_url")
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(x => x.IsPrimary)
                .HasColumnName("is_primary");

            entity.HasOne(x => x.Product)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.ProductId)
                .IsUnique()
                .HasFilter("[is_primary] = 1");
        });
        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.ToTable("Roles", "dbo");
            entity.HasKey(r => r.RoleId);
            entity.Property(r => r.Description).HasMaxLength(255);

            entity.Property(r => r.RoleName)
                .HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users", "dbo");
            entity.HasKey(u => u.UserId);

            entity.Property(u => u.UserId)
                .ValueGeneratedOnAdd();

            entity.Property(u => u.Username)
                .HasMaxLength(50).IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(255).IsRequired();

            entity.Property(u => u.PasswordHash)
                .HasMaxLength(255).IsRequired();

            entity.Property(u => u.Status)
                .HasMaxLength(20).IsRequired();

            entity.HasIndex(u => u.Username)
                .IsUnique();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CustomerProfile>(entity =>
        {
            entity.ToTable("CustomerProfiles", "dbo");
            entity.HasKey(p => p.CustomerProfileId);
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.ToTable("Orders", "dbo");
            entity.HasKey(o => o.OrderId);
        });

        modelBuilder.Entity<TestDriveBooking>(entity =>
        {
            entity.ToTable("TestDriveBookings", "dbo");
            entity.HasKey(b => b.BookingId);
        });
    }
}
