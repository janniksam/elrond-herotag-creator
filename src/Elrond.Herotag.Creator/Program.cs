using Elrond.Herotag.Creator.Web.BotWorkflows;
using Elrond.Herotag.Creator.Web.BotWorkflows.UserState;
using Elrond.Herotag.Creator.Web.Services;
using Elrond.Herotag.Creator.Web.Utils;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.local.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

builder.Services.AddSingleton<IConfiguration>(configuration);
builder.Services.AddMemoryCache();
builder.Services.AddLogging(b =>
{
    b.AddConsole();
    var loggingSection = configuration.GetSection("Logging");
    b.AddFile(loggingSection,
        fileLoggerOpts =>
        {
            fileLoggerOpts.FormatLogFileName = fName => string.Format(fName, DateTime.UtcNow);
        });
});

// cache
builder.Services.AddSingleton<IBotManager, BotManager>();
builder.Services.AddSingleton<IUserContextManager, UserContextManager>();
builder.Services.AddTransient<IElrondApiService, ElrondApiService>();
builder.Services.AddTransient<ITransactionGenerator, TransactionGenerator>();
builder.Services.AddHostedService<ElrondHerotagCreatorBotService>();

var app = builder.Build();
LoggingFactory.LogFactory = app.Services.GetService<ILoggerFactory>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{ 
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.Run();
