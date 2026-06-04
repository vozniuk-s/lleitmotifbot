using backend.Services;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace backend.Commands
{
    public class CommandExecutor
    {
        private readonly IEnumerable<ITelegramCommand> _commands;
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger _logger;
        private readonly MessageCacheService _message;
        private readonly TikTokDownloaderService _tiktokservice;

        public CommandExecutor(IEnumerable<ITelegramCommand> commands, ITelegramBotClient botClient, ILogger<CommandExecutor> logger, 
            MessageCacheService message, TikTokDownloaderService tiktokservice)
        {   
            _commands = commands;
            _botClient = botClient;
            _logger = logger;
            _message = message;
            _tiktokservice = tiktokservice;
        }

        public async Task ExecuteAsync(Update update)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text == null)
                return;

            var messageText = update.Message.Text;
            var chatId = update.Message.Chat.Id;
             
            if(update.Message.Text == "/dick@pipisabot")
            {
                await _botClient.DeleteMessage(chatId, update.Message.Id);
                return;
            }

            string pattern = @"https?://(www\.|vm\.|vt\.)?tiktok\.com/\S+";
            Match match = Regex.Match(messageText, pattern);

            if (match.Success)
            {
                string tiktokUrl = match.Value;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var mediaData = await _tiktokservice.GetTikTokDataAsync(tiktokUrl);

                        if (mediaData != null)
                        {
                            if (mediaData.Images != null && mediaData.Images.Count > 0)
                            {
                                var imagesToDownload = mediaData.Images.Take(10).ToList();

                                var downloadTasks = imagesToDownload.Select(async imageUrl =>
                                {
                                    var stream = await _tiktokservice.GetFileStreamAsync(imageUrl);
                                    if (stream != null)
                                    {
                                        return new InputMediaPhoto(InputFile.FromStream(stream, Guid.NewGuid() + ".jpg"));
                                    }
                                    return null;
                                });

                                var downloadedPhotos = await Task.WhenAll(downloadTasks);

                                var mediaGroup = downloadedPhotos
                                    .Where(photo => photo != null)
                                    .Cast<IAlbumInputMedia>()
                                    .ToList();

                                if (mediaGroup.Count > 0)
                                {
                                    await _botClient.SendMediaGroup(
                                        chatId: chatId,
                                        media: mediaGroup,
                                        disableNotification: true,
                                        replyParameters: new ReplyParameters { MessageId = update.Message.Id });
                                }
                            }
                        }
                        else if (!string.IsNullOrEmpty(mediaData.Play))
                        {
                            using var videoStream = await _tiktokservice.GetFileStreamAsync(mediaData.Play);

                            if (videoStream != null)
                            {
                                await _botClient.SendVideo(
                                    chatId: chatId,
                                    video: InputFile.FromStream(videoStream, "video.mp4"),
                                    disableNotification: true,
                                    replyParameters: new ReplyParameters { MessageId = update.Message.Id });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while background downloading TikTok");
                    }
                });
            }

            if (!messageText.StartsWith("/"))
                return;

            _logger.LogInformation("Chat: {ChatId} | User: {UserId} ({Username}) | Message: {MessageText}",
               chatId,
               update.Message.From?.Id,
               update.Message.From?.Username ?? "NoUsername",
               messageText);

            var commandString = messageText.Split(' ')[0].Split('@')[0];
            var command = _commands.FirstOrDefault(c =>
                string.Equals(c.Name, commandString, StringComparison.OrdinalIgnoreCase));

            if (command != null)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await command.ExecuteAsync(_botClient, update);
                    stopwatch.Stop();
                    _logger.LogInformation("Success command: {CommandName} | Time: {ElapsedMs} ms", command.Name, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex, "Error executing command {Command} in chat {ChatId} after {ElapsedMs} ms", messageText, chatId, stopwatch.ElapsedMilliseconds);
                }
            }
            else
            {
                _logger.LogInformation("Command not recognized: {MessageText} in chat {ChatId}", messageText, chatId);
                await _botClient.SendMessage(chatId, _message.GetMessage("UnknownCommand"));
            }
        }
    }
}