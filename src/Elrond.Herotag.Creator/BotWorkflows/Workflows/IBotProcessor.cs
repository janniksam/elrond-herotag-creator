﻿using Telegram.Bot;
using Telegram.Bot.Types;

namespace Elrond.Herotag.Creator.Web.BotWorkflows.Workflows;

public interface IBotProcessor
{
    Task<WorkflowResult> ProcessCallbackQueryAsync(ITelegramBotClient client, CallbackQuery query, CancellationToken ct);

    Task<WorkflowResult> ProcessMessageAsync(ITelegramBotClient client, Message message, CancellationToken ct);
}