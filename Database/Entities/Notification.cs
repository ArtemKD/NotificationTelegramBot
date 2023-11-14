namespace NotificationTelegramBot.Database.Entities
{
	public class Notification
	{
		public Guid Id { get; set; }

		public DateTime TriggerDate { get; set; }

		public bool IsTriggered { get; set; }
	}
}
