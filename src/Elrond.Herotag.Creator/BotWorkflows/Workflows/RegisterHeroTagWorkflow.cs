using System.Text.RegularExpressions;
using Elrond.Herotag.Creator.Web.BotWorkflows.UserState;
using Elrond.Herotag.Creator.Web.Extensions;
using Elrond.Herotag.Creator.Web.Services;
using Erdcsharp.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Elrond.Herotag.Creator.Web.BotWorkflows.Workflows;

public class RegisterHeroTagWorkflow : IBotProcessor
{
    private const string SelectNetworkQueryPrefix = "NWSelected_";

    private readonly IUserContextManager _userContextManager;
    private readonly IRegisterHerotagInputManager _registerHerotagInputManager;
    private readonly IElrondApiService _elrondApiService;
    private readonly ITransactionGenerator _transactionGenerator;

    public RegisterHeroTagWorkflow(
        IUserContextManager userContextManager, 
        IRegisterHerotagInputManager registerHerotagInputManager,
        IElrondApiService elrondApiService,
        ITransactionGenerator transactionGenerator)
    {
        _userContextManager = userContextManager;
        _registerHerotagInputManager = registerHerotagInputManager;
        _elrondApiService = elrondApiService;
        _transactionGenerator = transactionGenerator;
    }

    public async Task<WorkflowResult> ProcessCallbackQueryAsync(ITelegramBotClient client, CallbackQuery query, CancellationToken ct)
    {
        if (query.Message == null ||
            query.Data == null)
        {
            return WorkflowResult.Unhandled();
        }

        var userId = query.From.Id;
        var chatId = query.Message.Chat.Id;
        var previousMessageId = query.Message.MessageId;
        if (query.Data == CommonQueries.RegisterHerotag)
        {
            _registerHerotagInputManager.SetNetwork(userId, null);
            await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
            return await RegisterHerotagAsync(client, userId, chatId, null, ct);
        }

        if (query.Data.StartsWith(SelectNetworkQueryPrefix))
        {
            await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);

            var networkRaw = query.Data[SelectNetworkQueryPrefix.Length..];
            if(!Enum.TryParse(networkRaw, false, out Network network))
            {
                await RegisterHerotagAsync(client, userId, chatId, $"Cannot parse the selected network {networkRaw}", ct);
            }

            return await SetNetworkAsync(client, userId, chatId, network, ct);
        }


        return WorkflowResult.Unhandled();
    }

    private async Task<WorkflowResult> SetNetworkAsync(ITelegramBotClient client, long userId, long chatId, Network network,
        CancellationToken ct)
    {
        _registerHerotagInputManager.SetNetwork(userId, network);
        return await RegisterHerotagAsync(client, userId, chatId, null, ct);
    }

    public async Task<WorkflowResult> ProcessMessageAsync(ITelegramBotClient client, Message message, CancellationToken ct)
    {
        if (message.From == null)
        {
            return WorkflowResult.Unhandled();
        }

        if (message.Type != MessageType.Text)
        {
            return WorkflowResult.Unhandled();
        }

        var messageText = message.Text;
        var userId = message.From.Id;
        var chatId = message.Chat.Id;

        var (context, oldMessageId, _) = _userContextManager.Get(userId);
        if (context == UserContext.EnterHerotag)
        {
            await client.TryDeleteMessageAsync(chatId, oldMessageId, ct);
            return await OnHerotagEnteredAsync(client, userId, chatId, messageText, ct);
        }

        return WorkflowResult.Unhandled();
    }

    private async Task<WorkflowResult> RegisterHerotagAsync(ITelegramBotClient client,
        long userId,
        long chatId,
        string? errorFromPreviousTry,
        CancellationToken ct)
    {
        var network = _registerHerotagInputManager.GetNetwork(userId);
        var messageText = errorFromPreviousTry == null ? string.Empty : $"{errorFromPreviousTry}\r\n";
        if (network == null)
        {
            messageText += "You want to register a herotag.\r\nPlease enter the network that you want to register your herotag on:";
            await client.SendTextMessageAsync(
                chatId,
                messageText,
                replyMarkup: new InlineKeyboardMarkup(
                    new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("MainNet", $"{SelectNetworkQueryPrefix}MainNet"),
                            InlineKeyboardButton.WithCallbackData("TestNet", $"{SelectNetworkQueryPrefix}TestNet"),
                            InlineKeyboardButton.WithCallbackData("DevNet", $"{SelectNetworkQueryPrefix}DevNet")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Go back", CommonQueries.BackToHome)
                        }
                    }),
                cancellationToken: ct);
            return WorkflowResult.Handled();
        }

        messageText += $"You want to register a herotag for the network {network}.\r\nPlease enter the herotag that you want to register:";
        var message = await client.SendTextMessageAsync(
            chatId,
            messageText,
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Go back", CommonQueries.BackToHome)),
            cancellationToken: ct);
        return WorkflowResult.Handled(UserContext.EnterHerotag, message.MessageId);
    }

    private async Task<WorkflowResult> OnHerotagEnteredAsync(ITelegramBotClient client, long userId, long chatId, string? messageText, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(messageText))
        {
            return await RegisterHerotagAsync(client, userId, chatId, "The herotag you entered is empty.", ct);
        }

        var network = _registerHerotagInputManager.GetNetwork(userId);
        if (!network.HasValue)
        {
            return await RegisterHerotagAsync(client, userId, chatId, "The network has not been chosen yet.", ct);
        }

        var chosenHerotag = messageText.Replace(".elrond", string.Empty);
        var regex = new Regex("^[a-z0-9]{3,25}$");
        if (!regex.IsMatch(chosenHerotag))
        {
            return await RegisterHerotagAsync(client, userId, chatId, "The herotag...\r\n" +
                                                                    "- ... needs to be between 3 and 25 characters long\r\n" +
                                                                    "- ... must include alphanumerical characters only\r\n" +
                                                                    "- ... lower-case only\r\n", ct);
        }

        var isAvailable = await _elrondApiService.IsHerotagAvailable(chosenHerotag, network.Value);
        if (!isAvailable)
        {
            return await RegisterHerotagAsync(client, userId, chatId, "The herotag is already taken. Please choose different one.\r\n", ct);
        }

        var reclaimUrl = await _transactionGenerator.GenerateRegisterHerotagUrlAsync(chosenHerotag, network.Value);
        var buttons = new List<InlineKeyboardButton[]>
        {
            new[]
            {
                InlineKeyboardButton.WithUrl("Create herotag now", reclaimUrl),
                InlineKeyboardButton.WithCallbackData("Back", CommonQueries.BackToHome)
            }
        };

        await client.SendTextMessageAsync(chatId, "To finish the creation of the herotag, open the generated url below:", ParseMode.Html,
            disableWebPagePreview: true,
            disableNotification: true,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
        return WorkflowResult.Handled();
    }
}