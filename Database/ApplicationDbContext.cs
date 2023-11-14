using Microsoft.EntityFrameworkCore;
using NotificationTelegramBot.Database.Entities;
using Telegram.Bot.Types;

namespace NotificationTelegramBot.Database
{
	public class ApplicationDbContext : DbContext
	{
		public DbSet<Notification> Notifications { get; set; }
		public DbSet<NotificationMessage> NotificationMessages { get; set; }
		public DbSet<User> Users { get; set; }

		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
		{
			Database.EnsureCreated();
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			var dateNotification = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month == 12 ? 1 : DateTime.UtcNow.Month + 1, 1, 0, 0, 0, DateTimeKind.Utc);

			modelBuilder.Entity<Notification>().HasData(
				new Notification
				{
					Id = Guid.NewGuid(),
					TriggerDate = dateNotification,
					IsTriggered = false
				});

			modelBuilder.Entity<NotificationMessage>().HasData(
				new NotificationMessage
				{
					Id = Guid.NewGuid(),
					Message = "Необходимо оплатить vpn."
				});

			base.OnModelCreating(modelBuilder);
		}
	}
}
