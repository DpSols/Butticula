using JsonServices;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;
using File = System.IO.File;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMemoryCache();
using IHost host = builder.Build();

IMemoryCache cache =
    host.Services.GetRequiredService<IMemoryCache>();

JsonServiceFactory jsonServiceFactory = new JsonServiceFactory();
var prettifier = jsonServiceFactory.GetPrettifier;
var fileFormatter = jsonServiceFactory.GetFileService;

var botClient = new TelegramBotClient("1233647183:AAGkYmFm-fEUpdVxFEpX8Cf6hCmRUYPNw9c");

InlineKeyboardMarkup inlineKeyboardMarkup = new InlineKeyboardMarkup(new[]
{
    InlineKeyboardButton.WithCallbackData("Request", "req"),
    InlineKeyboardButton.WithCallbackData("Response", "resp"),
    InlineKeyboardButton.WithCallbackData("Custom", "custom")
});

//await botClient.DeleteWebhookAsync();
using CancellationTokenSource cts = new ();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new ()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);
var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    try
    {
        await (update.Type switch
        {
            UpdateType.Message => BotOnMessageReceived(bot, update.Message!, cancellationToken),
            UpdateType.CallbackQuery => BotOnCallbackQueryReceived(bot, update.CallbackQuery!, cancellationToken),
            _ => Task.CompletedTask
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception while handling {update.Type}: {ex}");
    }
}

async Task SendInlineKeyboard(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
{
    await bot.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "Choose a file type",
        replyMarkup: inlineKeyboardMarkup,
        cancellationToken: cancellationToken);
}

async Task SendMessage(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
{
    await bot.SendTextMessageAsync(
        chatId: chatId,
        text: text,
        cancellationToken: cancellationToken);
}

async Task BotOnMessageReceived(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
{
    if (message.Text == "/start")
    {
        await SendMessage(bot, message.Chat.Id, "Hi!/\nSend me json and choose how to name it and I will send you pretty json file!", cancellationToken);

        return;
    }

    if (message.Text != null)
    {
        string jsonText = message.Text;
        
        Console.WriteLine($"Received a {jsonText} message in chat {message.Chat.Id}.");
        string prettyText;
        
        try
        {
            prettyText = prettifier.SetText(jsonText).Prettify(); // Pretty message
        }
        catch (Exception e)
        {
            await SendMessage(bot, message.Chat.Id, "Error in parsing json, check the text provided",
                cancellationToken);
            Console.WriteLine(e);
            
            return;
        }
        
        try
        {
            fileFormatter.FileWriter(prettyText, message.Chat.Id.ToString());
            
            await SendInlineKeyboard(bot, message, cancellationToken);
        }
        catch (Exception e)
        {
            await SendMessage(bot, message.Chat.Id, "Error in saving into file. Try again.", cancellationToken);
            Console.WriteLine(e);

            return;
        }
    }
}

async Task BotOnCallbackQueryReceived(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken cancellationToken)
{
    string fName = callbackQuery.Data switch
    {
        "req" => "request",
        "resp" => "response",
        "custom" => "",
        _ => throw new ArgumentOutOfRangeException()
    };

    // if (fName == "")
    // {
    //     await SendMessage(bot, callbackQuery.Message.Chat.Id, "Type file name", cancellationToken);
    //     cache.Set()
    //     var message = update[0].Message;
    //     if (message != null) fName = message.Text;
    //         else await SendMessage(bot, callbackQuery.Message.Chat.Id, "Invalid name", cancellationToken);
    //     
    //     return;
    // }
    
    var prettyFilePath = fileFormatter.GetFilePath(callbackQuery.Message.Chat.Id.ToString());
            
    await using Stream stream = File.OpenRead(prettyFilePath);
    await bot.SendDocumentAsync(
        chatId: callbackQuery.Message.Chat.Id,
        document: InputFile.FromStream(stream: stream, fileName: fName + ".json"));

    stream.Close();;
}

Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    var errorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(errorMessage);
    return Task.CompletedTask;
}