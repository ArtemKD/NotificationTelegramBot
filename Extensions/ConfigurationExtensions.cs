using Microsoft.Extensions.Options;
using NotificationTelegramBot.Configurations;

namespace NotificationTelegramBot.Extensions
{
	public static class ConfigurationExtensions
	{
		public static void AddConfigurations(this IServiceCollection services, IConfiguration configuration)
		{
			var botConfigurationSection = configuration.GetSection(BotConfiguration.Configuration);

			services.Configure<BotConfiguration>(botConfigurationSection);
		}

		public static T GetConfiguration<T>(this IServiceProvider serviceProvider)
			where T: class
		{
			var options = serviceProvider.GetService<IOptions<T>>() ?? throw new ArgumentNullException(nameof(T));
			return options.Value;
		}
	}
}
