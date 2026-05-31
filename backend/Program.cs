using backend.Commands;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder();

var botToken = builder.Configuration["BotToken"];

builder.Services.AddControllers();
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddTransient<CommandExecutor>();

var app = builder.Build();

var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
string webhookUrl = "https://43ppzxbv-5103.euw.devtunnels.ms/api/bot";
await botClient.SetWebhook(webhookUrl);

app.MapControllers();

app.Run();

Console.WriteLine("Success");