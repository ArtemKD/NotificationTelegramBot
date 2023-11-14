using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotificationTelegramBot.Configurations;
using NotificationTelegramBot.Database;
using NotificationTelegramBot.Database.Entities;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace NotificationTelegramBot.Services
{
	public class UpdateHandlers
	{
		private readonly ILogger<UpdateHandlers> _logger;
		private readonly ITelegramBotClient _botClient;
		private readonly ApplicationDbContext _context;
		private readonly List<long> _adminList;

		public UpdateHandlers(ILogger<UpdateHandlers> logger,
			ITelegramBotClient botClient,
			ApplicationDbContext context,
			IOptions<BotConfiguration> botOptions)
		{
			_logger = logger;
			_botClient = botClient;
			_context = context;
			_adminList = botOptions.Value.AdminUsers;
		}

		public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
		{
			_logger.LogInformation("Update received: {@update}", update);
			var handle = update switch
			{
				{ Message: { } message } => OnMessageReceivedAsync(message, cancellationToken),
				_ => UnknownUpdateHandlerAsync(update, cancellationToken)
			};

			await handle;
		}

		private async Task OnMessageReceivedAsync(Message message, CancellationToken cancellationToken)
		{
			_logger.LogInformation("On message received {@message}.", message);
			StringBuilder stringBuilder;

			if (message.From is { } user)
			{
				if (message.Text is { } text)
				{
					switch (text)
					{
						case "/start":
							var a = _context.NotificationMessages.z;

							stringBuilder = new StringBuilder();

							stringBuilder.AppendLine("Подпишитесь на рассылку уведомлений об оплате используя команду /subscribe .");
							stringBuilder.AppendLine("Отмените подписку на рассылку с помощью команды /unsubscribe .");

							if (_adminList.Contains(user.Id))
							{
								stringBuilder.AppendLine("Сделать оповещение по всем подписанным пользователям /notification [ сообщение для отправки ] .");
								stringBuilder.AppendLine("Список подписчиков /users .");
								stringBuilder.AppendLine("Установить сообщение для отправки подписаными пользователям /notificationMessage [ сообщение для отправки ].");
							}

							await _botClient.SendTextMessageAsync(
								user.Id,
								text: stringBuilder.ToString(),
								cancellationToken: cancellationToken);

							stringBuilder.Clear();
							break;

						case string command when command.StartsWith("/notification "):
							if (_adminList.Contains(user.Id))
							{
								var notificationMessage = string.Join(" ", command.Split(' ').Skip(1));
								if (string.IsNullOrEmpty(notificationMessage))
								{
									await _botClient.SendTextMessageAsync(
										user.Id,
										text: "Ошибка. Пустое сообщение уведомления.",
										cancellationToken: cancellationToken);
									break;
								}

								var users = await _context.Users.ToListAsync();
								foreach (var u in users)
								{
									await _botClient.SendTextMessageAsync(
										u.Id,
										text: notificationMessage,
										cancellationToken: cancellationToken);
								}
							}
							break;

						case "/users":
							if (_adminList.Contains(user.Id))
							{
								var users = await _context.Users.ToListAsync();
								stringBuilder = new StringBuilder();
								stringBuilder.AppendLine("Подписчики: ");
								foreach (var u in users)
								{
									stringBuilder.AppendLine($"[{u.Id}] @{u.Username} - {u.FirstName} {u.LastName}");
								}

								await _botClient.SendTextMessageAsync(
									user.Id,
									text: stringBuilder.ToString(),
									cancellationToken: cancellationToken);
							}
							break;

						case string notificationMessageCommand when notificationMessageCommand.StartsWith("/notificationMessage"):
							if (_adminList.Contains(user.Id))
							{
								var notificationTemplate = string.Join(" ", notificationMessageCommand.Split(' ').Skip(1));
								if (string.IsNullOrEmpty(notificationTemplate))
								{
									await _botClient.SendTextMessageAsync(
										user.Id,
										text: "Ошибка. Пустое сообщение уведомления.",
										cancellationToken: cancellationToken);
									break;
								}

								var notificationMessage = await _context.NotificationMessages.FirstOrDefaultAsync();
								if (notificationMessage != null)
								{
									_context.NotificationMessages.Remove(notificationMessage);
									await _context.SaveChangesAsync();
								}

								await _context.NotificationMessages.AddAsync(new NotificationMessage
								{
									Message = notificationTemplate
								});
								await _context.SaveChangesAsync();

								await _botClient.SendTextMessageAsync(
										user.Id,
										text: "Сообщение успешно сохранено.",
										cancellationToken: cancellationToken);
							}
							break;

						case "/subscribe":
							var existedUser = await _context.Users.Where(e => e.Id == user.Id).FirstOrDefaultAsync();
							if (existedUser != null)
							{
								_logger.LogInformation("User {@user} already subscribe.", user);
								await _botClient.SendTextMessageAsync(
									user.Id,
									text: "Вы уже подписаны.",
									cancellationToken: cancellationToken);
								break;
							}

							await _context.Users.AddAsync(user);
							await _context.SaveChangesAsync();

							await _botClient.SendTextMessageAsync(
								user.Id,
								text: "Вы подписались.",
								cancellationToken: cancellationToken);
							break;

						case "/unsubscribe":
							existedUser = await _context.Users.Where(e => e.Id == user.Id).FirstOrDefaultAsync();
							if (existedUser == null)
							{
								_logger.LogInformation("User {@user} not exists.", user);
								await _botClient.SendTextMessageAsync(
									user.Id,
									text: "Вы не подписаны.",
									cancellationToken: cancellationToken);
								break;
							}

							_context.Users.Remove(existedUser);
							await _context.SaveChangesAsync();

							await _botClient.SendTextMessageAsync(
								user.Id,
								text: "Вы отписались.",
								cancellationToken: cancellationToken);
							break;

						default:
							_logger.LogInformation("Command: {command} not exists", text);
							await _botClient.SendTextMessageAsync(
								user.Id,
								text: "Не привильная команда.",
								cancellationToken: cancellationToken);
							break;
					}
				}
			}
			else
			{
				_logger.LogWarning("Receive message from unknown user {@message}", message);
			}
		}

		private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
		{
			_logger.LogInformation("Unknown update received {@update}", update);
			return Task.CompletedTask;
		}
	}
}
