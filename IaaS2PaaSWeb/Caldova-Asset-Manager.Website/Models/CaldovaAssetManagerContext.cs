using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CaldovaAssetManager.Models;

public class CaldovaAssetManagerContext(DbContextOptions<CaldovaAssetManagerContext> options)
    : IdentityDbContext<ApplicationUser>(options), ICaldovaAssetManagerContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<Raincheck> RainChecks { get; set; }
    public DbSet<Store> Stores { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Product>().ToTable("Products").Ignore(p => p.ProductDetailList);
        builder.Entity<Order>().ToTable("Orders");
        builder.Entity<Category>().ToTable("Categories");
        builder.Entity<CartItem>().ToTable("CartItems");
        builder.Entity<OrderDetail>().ToTable("OrderDetails");
        builder.Entity<Raincheck>().ToTable("Rainchecks");
        builder.Entity<Store>().ToTable("Stores");
        builder.Entity<Raincheck>().HasOne(r => r.IssuerStore).WithMany().HasForeignKey(r => r.StoreId);
        builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        builder.Entity<Product>().Property(p => p.SalePrice).HasPrecision(18, 2);
        builder.Entity<Order>().Property(o => o.Total).HasPrecision(18, 2);
        builder.Entity<OrderDetail>().Property(o => o.UnitPrice).HasPrecision(18, 2);

        // Map to the existing ASP.NET Identity 2 schema without changing SQL tables.
        var user = builder.Entity<ApplicationUser>();
        user.Ignore(u => u.NormalizedUserName);
        user.Ignore(u => u.NormalizedEmail);
        user.Ignore(u => u.ConcurrencyStamp);
        user.Property(u => u.Id).HasMaxLength(128);
        user.Property(u => u.UserName).HasMaxLength(256).IsRequired();
        user.Property(u => u.Email).HasMaxLength(256);
        user.Property(u => u.LockoutEnd).HasColumnName("LockoutEndDateUtc")
            .HasConversion(value => value.HasValue ? value.Value.UtcDateTime : (DateTime?)null,
                value => value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null)
            .HasColumnType("datetime");
        var role = builder.Entity<IdentityRole>();
        role.Ignore(r => r.NormalizedName);
        role.Ignore(r => r.ConcurrencyStamp);
        role.Property(r => r.Id).HasMaxLength(128);
        role.Property(r => r.Name).HasMaxLength(256).IsRequired();
        builder.Entity<IdentityUserLogin<string>>().Ignore(l => l.ProviderDisplayName);
        builder.Entity<IdentityUserLogin<string>>().HasKey(l => new { l.LoginProvider, l.ProviderKey, l.UserId });
        builder.Entity<IdentityUserLogin<string>>().Property(l => l.LoginProvider).HasMaxLength(128);
        builder.Entity<IdentityUserLogin<string>>().Property(l => l.ProviderKey).HasMaxLength(128);
        builder.Entity<IdentityUserLogin<string>>().Property(l => l.UserId).HasMaxLength(128);
        builder.Entity<IdentityUserRole<string>>().Property(r => r.UserId).HasMaxLength(128);
        builder.Entity<IdentityUserRole<string>>().Property(r => r.RoleId).HasMaxLength(128);
        builder.Entity<IdentityUserClaim<string>>().Property(c => c.UserId).HasMaxLength(128);
    }
}
