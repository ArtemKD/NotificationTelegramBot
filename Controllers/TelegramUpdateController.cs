using Microsoft.AspNetCore.Mvc;
using NotificationTelegramBot.Services;
using Telegram.Bot.Types;

namespace NotificationTelegramBot.Controllers
{
	public class TelegramUpdateController : ControllerBase
	{
		[HttpPost]
		public async Task<IActionResult> Update(
		[FromBody] Update update,
		[FromServices] UpdateHandlers telegramUpdateHandlers,
		CancellationToken cancellationToken)
		{
			await telegramUpdateHandlers.HandleUpdateAsync(update, cancellationToken);

			return Ok();
		}
	}
}
