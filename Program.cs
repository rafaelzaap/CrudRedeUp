using EvolutionSender.Data;
using EvolutionSender.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "EvolutionSender.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Conta/Login";
        options.LogoutPath = "/Conta/Logout";
        options.AccessDeniedPath = "/Conta/Login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(
            Path.Combine(builder.Environment.ContentRootPath, ".dotnet-appdata", "DataProtectionKeys")));
}
builder.Services.AddScoped<IMembroRepository, MembroRepository>();
builder.Services.AddScoped<IMensagemRepository, MensagemRepository>();
builder.Services.AddScoped<IEnvioMensagemHistoricoRepository, EnvioMensagemHistoricoRepository>();
builder.Services.AddScoped<IAniversarioEnvioHistoricoRepository, AniversarioEnvioHistoricoRepository>();
builder.Services.AddScoped<ISistemaConfiguracaoRepository, SistemaConfiguracaoRepository>();
builder.Services.AddScoped<IUsuarioSistemaRepository, UsuarioSistemaRepository>();
builder.Services.AddScoped<IDatabaseStartupMigration, DatabaseStartupMigration>();
builder.Services.AddScoped<IEnvioMensagemLock, MySqlEnvioMensagemLock>();
builder.Services.Configure<EvolutionApiOptions>(builder.Configuration.GetSection("EvolutionApi"));
builder.Services.Configure<EnvioMensagemOptions>(builder.Configuration.GetSection("EnvioMensagem"));
builder.Services.Configure<AniversarioOptions>(builder.Configuration.GetSection("Aniversario"));
builder.Services.AddScoped<IUsuarioAuthService, UsuarioAuthService>();
builder.Services.AddScoped<ISistemaStatusService, SistemaStatusService>();
builder.Services.AddScoped<IAniversarioService, AniversarioService>();
builder.Services.AddScoped<IEnvioMensagemService, EnvioMensagemService>();
builder.Services.AddSingleton<IEnvioMensagemJobQueue, EnvioMensagemJobQueue>();
builder.Services.AddSingleton<EnvioMensagemJobService>();
builder.Services.AddSingleton<IEnvioMensagemJobService>(serviceProvider =>
    serviceProvider.GetRequiredService<EnvioMensagemJobService>());
builder.Services.AddHostedService<EnvioMensagemBackgroundService>();
builder.Services.AddHostedService<AniversarioAutomaticoBackgroundService>();
builder.Services.AddHttpClient("EvolutionHealth", client =>
{
    client.Timeout = TimeSpan.FromSeconds(2);
});
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

using (var scope = app.Services.CreateScope())
{
    var migration = scope.ServiceProvider.GetRequiredService<IDatabaseStartupMigration>();
    await migration.ExecutarAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
