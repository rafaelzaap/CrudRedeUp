using EvolutionSender.Data;
using EvolutionSender.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IMembroRepository, MembroRepository>();
builder.Services.AddScoped<IMensagemRepository, MensagemRepository>();
builder.Services.Configure<EvolutionApiOptions>(builder.Configuration.GetSection("EvolutionApi"));
builder.Services.Configure<EnvioMensagemOptions>(builder.Configuration.GetSection("EnvioMensagem"));
builder.Services.AddScoped<IEnvioMensagemService, EnvioMensagemService>();
builder.Services.AddHttpClient<IEvolutionApiClient, EvolutionApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<EvolutionApiOptions>>()
        .Value;

    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
