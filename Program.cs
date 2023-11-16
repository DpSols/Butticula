using FileExchanger;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMemoryCache();
using IHost host = builder.Build();

IMemoryCache cache =
    host.Services.GetRequiredService<IMemoryCache>();

var respController = new ResponseController(cache);

var botClient = new TelegramBotClient("1233647183:AAGkYmFm-fEUpdVxFEpX8Cf6hCmRUYPNw9c");

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
            UpdateType.Message => respController.BotOnMessageReceived(bot, update.Message!, cancellationToken),
            UpdateType.CallbackQuery => respController.BotOnCallbackQueryReceived(bot, update.CallbackQuery!, cancellationToken),
            _ => Task.CompletedTask
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception while handling {update.Type}: {ex}");
    }
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