using Elrond.Herotag.Creator.Web.BotWorkflows.UserState;
using Elrond.Herotag.Creator.Web.BotWorkflows.Workflows;
using Elrond.Herotag.Creator.Web.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Elrond.Herotag.Creator.Web.BotWorkflows
{
    public sealed class ElrondHerotagCreatorBotService : IHostedService, IDisposable
    {
        private readonly IBotManager _botManager;
        private readonly IUserContextManager _userContextManager;
        private readonly IRegisterHerotagInputManager _registerHerotagInputManager;
        private readonly ILogger<ElrondHerotagCreatorBotService> _logger;
        private readonly CancellationTokenSource _cts;
        private readonly IElrondApiService _elrondApiService;
        private readonly ITransactionGenerator _transactionGenerator;

        public ElrondHerotagCreatorBotService(
            IBotManager botManager,
            IUserContextManager userContextManager,
            IRegisterHerotagInputManager registerHerotagInputManager,
            IElrondApiService elrondApiService,
            ITransactionGenerator transactionGenerator,
            ILogger<ElrondHerotagCreatorBotService> logger)
        {
            _botManager = botManager;
            _userContextManager = userContextManager;
            _registerHerotagInputManager = registerHerotagInputManager;
            _elrondApiService = elrondApiService;
            _transactionGenerator = transactionGenerator;
            _logger = logger;
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _botManager.StartAsync(HandleUpdateAsync, HandleErrorAsync, _cts.Token);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken ct)
        {
            try
            {
                var botWorkflows = GetWorkflows();

                if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery?.From != null)
                {
                    foreach (var botProcessor in botWorkflows)
                    {
                        var workflowResult = await botProcessor.ProcessCallbackQueryAsync(client, update.CallbackQuery, ct);
                        if (!workflowResult.IsHandled)
                        {
                            continue;
                        }

                        var userId = update.CallbackQuery.From.Id;
                        _userContextManager.AddOrUpdate(userId, (workflowResult.NewUserContext, workflowResult.OldMessageId, workflowResult.AdditionalArgs));

                        await AnswerCallbackAsync(client, update, ct);
                        return;
                    }

                    return;
                }

                if (update.Type == UpdateType.Message &&
                    update.Message?.From != null)
                {
                    foreach (var botProcessor in botWorkflows)
                    {
                        var workflowResult = await botProcessor.ProcessMessageAsync(client, update.Message, ct);
                        if (!workflowResult.IsHandled)
                        {
                            continue;
                        }

                        var fromId = update.Message.From.Id;
                        _userContextManager.AddOrUpdate(fromId, (workflowResult.NewUserContext, workflowResult.OldMessageId, workflowResult.AdditionalArgs));
                        return;
                    }
                }
            }
            catch (ApiRequestException ex)
            {
                _logger.LogError(ex, "Unexpected ApiRequestException.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception.");
            }
        }

        private IEnumerable<IBotProcessor> GetWorkflows()
        {
            var startmenuWorkflow = new StartMenuWorkflow();
            var botWorkflows = new IBotProcessor[]
            {
                startmenuWorkflow,
                new RegisterHeroTagWorkflow(_userContextManager, _registerHerotagInputManager, _elrondApiService, _transactionGenerator)
            };

            return botWorkflows;
        }

        private static async Task AnswerCallbackAsync(ITelegramBotClient client, Update update, CancellationToken ct)
        {
            if (update.CallbackQuery == null)
            {
                return;
            }

            try
            {
                await client.AnswerCallbackQueryAsync(update.CallbackQuery.Id, cancellationToken: ct);
            }
            catch (ApiRequestException)
            {
                // can crash on server-restarts
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken ct)
        {
            _logger.LogError(exception, "An unhandled telegram exception has occured");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}
