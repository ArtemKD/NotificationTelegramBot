using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NotificationTelegramBot.Database;
using NotificationTelegramBot.Database.Entities;
using Telegram.Bot;

namespace NotificationTelegramBot.Services
{
	public class NotificationService: IHostedService, IDisposable
	{
		private readonly ILogger<NotificationService> _logger;
		private Timer? _notificationTimer;
		private readonly AutoResetEvent _autoResetEvent;
		private readonly ITelegramBotClient _botClient;
		private readonly IServiceScopeFactory _scopeFactory;

		public NotificationService(
			ILogger<NotificationService> logger,
			ITelegramBotClient botClient,
			IServiceScopeFactory scopeFactory)
		{
			_logger = logger;
			_autoResetEvent = new AutoResetEvent(false);
			_botClient = botClient;
			_scopeFactory = scopeFactory;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Start timer.");
			//_notificationTimer = new Timer(SendNotificationAsync, _autoResetEvent, 0, (int)TimeSpan.FromMinutes(10).TotalMilliseconds);
			_notificationTimer = new Timer(SendNotificationAsync, _autoResetEvent, 0, (int)TimeSpan.FromSeconds(2).TotalMilliseconds);
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Stop timer.");
			_notificationTimer?.Change(Timeout.Infinite, 0);
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			_notificationTimer?.Dispose();
		}

		private async void SendNotificationAsync(object? state)
		{
			_logger.LogInformation("Check timer.");
			using (var scope = _scopeFactory.CreateScope())
			{
				var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
				var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

				var lastNotification = cache.Get<Notification>(typeof(Notification));

				if (lastNotification == null)
				{
					_logger.LogInformation("Out of cache.");
					lastNotification = await context.Notifications.OrderBy(e => e.TriggerDate).FirstOrDefaultAsync();

					if (lastNotification == null)
					{
						_logger.LogInformation("Empty notification database.");
						var startNotification = new Notification
						{
							//TriggerDate = DateTime.UtcNow + TimeSpan.FromDays(30),
							TriggerDate = DateTime.UtcNow + TimeSpan.FromSeconds(10),
						};

						await context.Notifications.AddAsync(startNotification);
						await context.SaveChangesAsync();

						lastNotification = startNotification;
					}

					cache.Set(typeof(Notification), lastNotification);
				}

				_logger.LogInformation("Notification triggered. Now date: {nowDate}, Trigger date: {triggerDate}.", DateTime.UtcNow, lastNotification.TriggerDate);

				if (lastNotification.TriggerDate < DateTime.UtcNow)
				{
					var users = await context.Users.ToListAsync();
					var notificationMessage = await context.NotificationMessages.OrderBy(e => e.Message).FirstOrDefaultAsync();
					if (notificationMessage != null)
					{
						foreach (var user in users)
						{
							await _botClient.SendTextMessageAsync(
								user.Id,
								text: notificationMessage.Message);
						}
					}
					else
					{
						_logger.LogWarning("Notification message not exists.");
					}

					var notificaton = await context.Notifications.FindAsync(lastNotification.Id);
                    if (notificaton != null)
                    {
						notificaton.IsTriggered = true;
                    }

					var newNotification = new Notification
					{
						//TriggerDate = DateTime.UtcNow + TimeSpan.FromDays(30),
						TriggerDate = DateTime.UtcNow + TimeSpan.FromSeconds(10),
					};

					_logger.LogInformation("New notification trigger: {notification}.", newNotification);

					await context.Notifications.AddAsync(newNotification);
					await context.SaveChangesAsync();

					cache.Set(typeof(Notification), newNotification);
				}
			}
		}
	}
}
