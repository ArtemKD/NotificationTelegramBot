using Microsoft.EntityFrameworkCore;
using NotificationTelegramBot.Configurations;
using NotificationTelegramBot.Database;
using NotificationTelegramBot.Services;
using System.Diagnostics;
using Telegram.Bot;

namespace NotificationTelegramBot.Extensions
{
	public static class CommonExtensions
	{
		public static void AddNotificationDatabase(this IServiceCollection services, IConfiguration configuration)
		{
			var connectionString = configuration.GetConnectionString("NotificationDb");
			services.AddDbContext<ApplicationDbContext>(opt =>
			{
				opt.UseSqlite(connectionString);
			});
		}

		public static void AddNotificationServices(this IServiceCollection services)
		{
			services.AddHostedService<NotificationService>();
		}

		public static void AddTelegramServices(this IServiceCollection services)
		{
			services.AddHostedService<WebHookTelegramService>();

			services.AddHttpClient("telegram_bot_client")
				.AddTypedClient<ITelegramBotClient>((httpClient,serviceProvider) =>
				{
					BotConfiguration botConfiguration = serviceProvider.GetConfiguration<BotConfiguration>();
					TelegramBotClientOptions botOptions = new(botConfiguration.BotToken);
					return new TelegramBotClient(botOptions, httpClient);
				});

			services.AddScoped<UpdateHandlers>();
		}

		public static ControllerActionEndpointConventionBuilder MapTelegramBotRoute<T>(this IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
			where T : class
		{
			var controllerName = typeof(T).Name.Replace("Controller", "", StringComparison.Ordinal);
			var actionName = typeof(T).GetMethods()[0].Name;

			var botConfiguration = serviceProvider.GetConfiguration<BotConfiguration>();

			return endpoints.MapControllerRoute(
				name: "TelegramBotUpdate",
				pattern: botConfiguration.Route,
				defaults: new
				{
					controller = controllerName,
					action = actionName
				});
		}
	}
}
