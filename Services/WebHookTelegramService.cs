using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using NotificationTelegramBot.Configurations;
using Microsoft.Extensions.Options;

namespace NotificationTelegramBot.Services
{
	public class WebHookTelegramService : IHostedService
	{
		private readonly ILogger<WebHookTelegramService> _logger;
		private readonly IServiceProvider _serviceProvider;
		private readonly BotConfiguration _configuration;

		public WebHookTelegramService(ILogger<WebHookTelegramService> logger, IServiceProvider serviceProvider, IOptions<BotConfiguration> botOptions)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
			_configuration = botOptions.Value;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			using var scope = _serviceProvider.CreateScope();
			var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

			var webhookAddress = $"{_configuration.HostAddress}{_configuration.Route}";
			_logger.LogInformation("Setting webhook: {WebhookAddress}", webhookAddress);

			_logger.LogInformation("Admin Users: {@users}", _configuration.AdminUsers);

			await botClient.SetWebhookAsync(
				url: webhookAddress,
				allowedUpdates: Array.Empty<UpdateType>(),
				secretToken: _configuration.SecretToken,
				cancellationToken: cancellationToken);
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			using var scope = _serviceProvider.CreateScope();
			var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

			_logger.LogInformation("Removing webhook");

			await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
		}
	}
}
