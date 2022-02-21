using EventStore.Client;
using Mum.Data;
using Mum.Pages.Index;

var builder = WebApplication.CreateBuilder(args);
ConfigureDIContainer(builder);

var application = builder.Build();
ConfigureRequestPipeline(application);
await Initialise(application);
application.Run();

/* ------------ Helpers ------------ */

void ConfigureDIContainer(WebApplicationBuilder build)
{
    build.Services.AddRazorPages();
    build.Services.AddServerSideBlazor();

    var settings = EventStoreClientSettings.Create(build.Configuration["ConnectionStrings:EventStore"]);
    build.Services.AddSingleton(new EventStoreClient(settings));

    build.Services.AddSingleton<AccountRepo>();
    build.Services.AddTransient<IndexViewModel>();
}

void ConfigureRequestPipeline(WebApplication app)
{
    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
    }

    app.UseStaticFiles();
    app.UseRouting();
    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");
}

async Task Initialise(WebApplication app)
{
    var repo = app.Services.GetService<AccountRepo>();

    if (repo is not null)
    {
        await repo.Initialise();
    }
}
