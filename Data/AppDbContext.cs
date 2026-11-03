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
        public DbSet<DeviceToken> DeviceTokens { get; set; }
        public DbSet<NotificationOutboxMessage> NotificationOutboxMessages { get; set; }
        public DbSet<FestivalSchedule> FestivalSchedules { get; set; }

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

            modelBuilder.Entity<Notification>()
                .HasIndex(notification => notification.EventKey)
                .IsUnique();

            modelBuilder.Entity<ProductRequest>()
                .HasIndex(request => request.IdempotencyKey)
                .IsUnique();

            modelBuilder.Entity<DeviceToken>()
                .HasIndex(deviceToken => deviceToken.TokenHash)
                .IsUnique();

            modelBuilder.Entity<DeviceToken>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(deviceToken => deviceToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotificationOutboxMessage>()
                .HasIndex(message => new { message.NotificationId, message.DeviceTokenId })
                .IsUnique();

            modelBuilder.Entity<NotificationOutboxMessage>()
                .HasOne(message => message.Notification)
                .WithMany()
                .HasForeignKey(message => message.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotificationOutboxMessage>()
                .HasOne(message => message.DeviceToken)
                .WithMany()
                .HasForeignKey(message => message.DeviceTokenId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            modelBuilder.Entity<NotificationOutboxMessage>()
                .HasIndex(message => new { message.NotificationId, message.Topic })
                .IsUnique()
                .HasFilter("\"Topic\" IS NOT NULL");

            modelBuilder.Entity<FestivalSchedule>()
                .HasIndex(festival => new { festival.Name, festival.FestivalDate })
                .IsUnique();
        }
    }
}