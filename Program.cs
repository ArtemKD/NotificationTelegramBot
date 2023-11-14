using NotificationTelegramBot.Controllers;
using NotificationTelegramBot.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Configuration
	.AddEnvironmentVariables();

builder.Services.AddSerilog((serviceProvider, configuration) =>
{
	configuration
		.MinimumLevel.Information()
		.MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Information)
		.MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
		.Enrich.FromLogContext()
		.Enrich.WithProperty("ApplicationName", "NotificationTelegramBot")
		.WriteTo.Console();
});

builder.Services.AddMemoryCache();

builder.Services.AddConfigurations(builder.Configuration);

builder.Services.AddNotificationDatabase(builder.Configuration);
builder.Services.AddNotificationServices();

builder.Services.AddTelegramServices();

builder.Services.AddControllers()
	.AddNewtonsoftJson();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

//app.UseTelegramSecretTokenVerification();
app.UseAuthorization();

app.MapTelegramBotRoute<TelegramUpdateController>(app.Services);
app.MapControllers();

app.Run();
