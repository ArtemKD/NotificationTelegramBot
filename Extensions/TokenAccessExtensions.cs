using Microsoft.Extensions.Options;
using NotificationTelegramBot.Configurations;

namespace NotificationTelegramBot.Extensions
{
	public static class TokenAccessExtensions
	{
		public static IApplicationBuilder UseTelegramSecretTokenVerification(this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<TelegramSecretTokenMiddleware>();
		}

		public class TelegramSecretTokenMiddleware
		{
			private readonly RequestDelegate _next;
			private readonly BotConfiguration _botConfiguration;

			public TelegramSecretTokenMiddleware(RequestDelegate next, IOptions<BotConfiguration> botOptions)
			{
				_next = next;
				_botConfiguration = botOptions.Value;
			}

			public async Task InvokeAsync(HttpContext context)
			{
				var token = context.Request.Headers[_botConfiguration.SecretToken];
				if (!token.Contains(_botConfiguration.SecretToken))
				{
					context.Response.StatusCode = 403;
					await context.Response.WriteAsync("Token is invalid");
				}
				else
				{
					await _next.Invoke(context);
				}
			}
		}
	}
}
