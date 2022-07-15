using Elrond.Herotag.Creator.Web.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Elrond.Herotag.Creator.Web.BotWorkflows.Workflows;

public class StartMenuWorkflow : IBotProcessor, IStartMenuNavigation
{
    private const string AboutQuery = "about";
    private const string AboutText = "Made with 💚 by [janniksam](https://twitter.com/janniksamc/)\n\n" +
                                     "**Source\\-Code**:\n" +
                                     "[GitHub Repository](https://github.com/janniksam/elrond-herotag-creator)";

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

        if (query.Data == AboutQuery)
        {
            await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
            await ShowAboutAsync(client, chatId, ct);
            return WorkflowResult.Handled();
        }
        
        if (query.Data == CommonQueries.BackToHome)
        {
            await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
            await ShowStartMenuAsync(client, userId, chatId, ct);
            return WorkflowResult.Handled();
        }

        return WorkflowResult.Unhandled();
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

        if (messageText is "/start" or "/menu")
        {
            await ShowStartMenuAsync(client, userId, chatId, ct);
            return WorkflowResult.Handled();
        }

        return WorkflowResult.Unhandled();
    }

    public async Task ShowStartMenuAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
    {
        var buttons = new List<InlineKeyboardButton[]>
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("➕ Register a herotag", CommonQueries.RegisterHerotag),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("💡 About the author", AboutQuery)
            }
        };

        await client.SendTextMessageAsync(
            chatId,
            "Welcome to the Elrond Herotag Creator.\n\n" +
            "This bot allows you to register a herotag for any Elrond wallet.\n\n" +
            "Please choose an action:",
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }

    private static async Task ShowAboutAsync(ITelegramBotClient client, long chatId, CancellationToken ct)
    {
        await client.SendTextMessageAsync(chatId,
            AboutText,
            ParseMode.MarkdownV2,
            disableWebPagePreview: true,
            replyMarkup: new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("Back", CommonQueries.BackToHome)),
            cancellationToken: ct);
    }
}