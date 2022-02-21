using EventStore.Client;
using Mum.Data;
using Mum.Pages.Index;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var settings = EventStoreClientSettings.Create(builder.Configuration["ConnectionStrings:EventStore"]);
builder.Services.AddSingleton<AccountRepo>();

builder.Services.AddSingleton<EventStoreClient>(new EventStoreClient(settings));
builder.Services.AddTransient<IndexViewModel>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Async initialisation
var repo = app.Services.GetService<AccountRepo>();

if (repo is not null)
{
    await repo.Initialise();
}

app.Run();
