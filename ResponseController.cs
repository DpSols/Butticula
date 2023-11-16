using System.Text.Json;
using JsonServices;
using JsonServices.Formatter;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FileExchanger;

public class ResponseController
{
    private readonly JsonServiceFactory _jsonServiceFactory = new();
    private readonly IPrettifier _prettifier;
    private readonly IFileService _fileFormatter;
    private readonly IMemoryCache _cache;

    public ResponseController(IMemoryCache cache)
    {
        _prettifier = _jsonServiceFactory.GetPrettifier;
        _fileFormatter = _jsonServiceFactory.GetFileService;
        _cache = cache;
    }

    private readonly InlineKeyboardMarkup _inlineKeyboardMarkup = new (new[]
    {
        InlineKeyboardButton.WithCallbackData("Request", "req"),
        InlineKeyboardButton.WithCallbackData("Response", "resp"),
        InlineKeyboardButton.WithCallbackData("Custom", "custom")
    });

    async Task SendInlineKeyboard(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        await bot.SendTextMessageAsync(
            chatId: chatId,
            text: "Choose a file type",
            replyMarkup: _inlineKeyboardMarkup,
            cancellationToken: cancellationToken);
    }

    async Task SendMessage(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        await bot.SendTextMessageAsync(
            chatId: chatId,
            text: text,
            cancellationToken: cancellationToken);
    }

    public async Task BotOnMessageReceived(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        long chatId = message.Chat.Id;
        if (message.Text == "/start")
        {
            await SendMessage(bot, chatId,
                "Hi!/\nSend me json and choose how to name it and I will send you pretty json file!",
                cancellationToken);

            return;
        }

        if (message.Text != null)
        {
            string messageText = message.Text;
            
            Console.WriteLine($"Received a {messageText} message in chat {chatId}.");
            
            if (_cache.TryGetValue<UserCacheEntity>(chatId, out var userCacheEntity))
            {
                switch (userCacheEntity.UserStatus)
                {
                    case UserStatus.WaitingForFilename:
                        char[] invalidFileNameChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

                        if (messageText.IndexOfAny(invalidFileNameChars) != -1 || messageText.All(c => c > 127))
                        {
                            await SendMessage(bot, chatId, "Give the valid file name. File can't contain any of the following characters: '\\\\', '/', ':', '*', '?', '\"', '<', '>', '|'",
                                cancellationToken);
                            return;
                        }
                        userCacheEntity.UserStatus = UserStatus.FileNamed;
                        await SendNamedFile(bot, cancellationToken, chatId, messageText);
                        return;
                }
            }
            
            string prettyText;

            if (IsValidJson(messageText))
            {
                try
                {
                    prettyText = _prettifier.SetText(messageText).Prettify(); // Pretty message
                }
                catch (Exception e)
                {
                    await SendMessage(bot, chatId, "Error in parsing json, check the text provided",
                        cancellationToken);

                    return;
                }

                _cache.Set<UserCacheEntity>(chatId, new UserCacheEntity(prettyText, UserStatus.JsonPrettified));
                SendInlineKeyboard(bot, chatId, cancellationToken);
            }
            else
            {
                await SendMessage(bot, chatId,
                    "Hi!\nSend me json and choose how to name it and I will send you pretty json file!",
                    cancellationToken);
            }
        }
    }

    public async Task BotOnCallbackQueryReceived(ITelegramBotClient bot, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        
        if (!_cache.TryGetValue<UserCacheEntity>(chatId, out var userCacheEntity))
        {
            await SendMessage(bot, chatId, "Send the json text first.", cancellationToken);

            return;
        }

        if (userCacheEntity.UserStatus != UserStatus.JsonPrettified)
        {
            await SendMessage(bot, chatId, "Send the json text first. We don't have json to send you in a file.", cancellationToken);
            
            return;
        }
        
        switch (callbackQuery.Data)
        {
            case "req":
                userCacheEntity.UserStatus = UserStatus.FileNamed;
                await SendNamedFile(bot, cancellationToken, chatId, "request");
                break;
            case "resp":
                userCacheEntity.UserStatus = UserStatus.FileNamed;
                await SendNamedFile(bot, cancellationToken, chatId, "response");
                break;
            case "custom":
                userCacheEntity.UserStatus = UserStatus.WaitingForFilename;
                await SendMessage(bot, chatId, "Send the json file name.", cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task SendNamedFile(ITelegramBotClient bot, CancellationToken cancellationToken,long chatId, string fName)
    {
        if (!_cache.TryGetValue<UserCacheEntity>(chatId, out var userCacheEntity))
        {
            await SendMessage(bot, chatId, "Send the json text first.", cancellationToken);

            return;
        }

        if (userCacheEntity.UserStatus != UserStatus.FileNamed)
        {
            throw new InvalidOperationException("User state should be fileNamed");
        }
        
        try
        {
            _fileFormatter.FileWriter(userCacheEntity.Json, chatId.ToString());
        }
        catch (Exception e)
        {
            await SendMessage(bot, chatId, "Error in saving into file. Try again.", cancellationToken);
            Console.WriteLine(e);
        
            return;
        }
        
        var prettyFilePath = _fileFormatter.GetFilePath(chatId.ToString());

        await using Stream stream = System.IO.File.OpenRead(prettyFilePath);
        await bot.SendDocumentAsync(
            chatId: chatId,
            document: InputFile.FromStream(stream: stream, fileName: fName + ".json"),
            cancellationToken: cancellationToken);

        stream.Close();
        _fileFormatter.FileRemove(chatId.ToString());
        _cache.Remove(chatId);
    }

    private static bool IsValidJson(string jsonString)
    {
        try
        {
            JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}