using Microsoft.EntityFrameworkCore;
using ShopAPI.Models;

namespace ShopAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        public DbSet<Category> Categories { get; set; }

       public DbSet<Sale> Sales { get; set; }

public DbSet<SaleItem> SaleItems { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        // Uncomment this
        public DbSet<User> Users { get; set; }

        public DbSet<ProductRequest> ProductRequests { get; set; }
        public DbSet<Pending> Pendings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<Product>()
        .Property(x => x.CreatedDate)
        .HasColumnType("timestamp with time zone")
        .HasConversion(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
        );

            modelBuilder.Entity<ProductRequest>()
                .HasOne(pr => pr.User)
                .WithMany()
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}