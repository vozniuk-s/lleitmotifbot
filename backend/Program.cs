using backend.Commands;
using Telegram.Bot;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Services;
using NLog.Web;

var startupLogger = NLog.LogManager.GetCurrentClassLogger();
startupLogger.Info("Bot loading...");

var builder = WebApplication.CreateBuilder();

builder.Logging.ClearProviders();
builder.Host.UseNLog();

var botToken = builder.Configuration["BotToken"];

builder.Services.AddDbContext<BotDbContext>(optins =>
    optins.UseSqlite("Data Source=bot.db"));

builder.Services.AddControllers();  
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

builder.Services.AddTransient<DickService>();
builder.Services.AddTransient<ITelegramCommand, StartCommand>();
builder.Services.AddTransient<ITelegramCommand, DickCommand>();
builder.Services.AddTransient<ITelegramCommand, TopCommand>();
builder.Services.AddTransient<ITelegramCommand, AdminCommand>();
builder.Services.AddTransient<ITelegramCommand, SetScoreCommand>();
builder.Services.AddTransient<ITelegramCommand, SetMessageCommand>();
builder.Services.AddTransient<ITelegramCommand, DeleteRecordCommand>();
builder.Services.AddTransient<ITelegramCommand, ResetAttemptCommand>();
builder.Services.AddTransient<CommandExecutor>();

builder.Services.Configure<backend.Models.AdminSettings>(builder.Configuration.GetSection("AdminSettings"));
builder.Services.AddSingleton<backend.Services.MessageCacheService>();

builder.Services.AddHostedService<BotBackgroundService>();

var app = builder.Build();

// init db
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<backend.Data.BotDbContext>();
    db.Database.EnsureCreated();

    var textCache = scope.ServiceProvider.GetRequiredService<backend.Services.MessageCacheService>();
    textCache.LoadCache();
}

/*var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
string webhookUrl = builder.Configuration["WebhookUrl"]
    ?? throw new ArgumentNullException("Can't find WebhookUrl in config");
await botClient.SetWebhook(webhookUrl);
*/
// Pipeline
app.UseMiddleware<backend.Middlewares.ExceptionHandlingMiddleware>();

app.MapControllers();

startupLogger.Info("Bot started.");

app.Run();

Console.WriteLine("Success");