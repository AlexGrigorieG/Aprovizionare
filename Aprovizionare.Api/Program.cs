using System.Text.Json.Serialization;
using Aprovizionare.Api.Data;
using Aprovizionare.Api.Entities;
using Aprovizionare.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddProblemDetails();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
           .AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>());
});


builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<AuditInterceptor>();
builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<GoodsReceiptService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddHttpClient<IExchangeRateProvider, BnrExchangeRateProvider>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Bnr:BaseUrl"] ?? "https://curs.bnr.ro/");
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<AlertOptions>(builder.Configuration.GetSection("Alerts"));

if (string.IsNullOrWhiteSpace(builder.Configuration["Email:SmtpHost"]))
    builder.Services.AddSingleton<IEmailSender, FileEmailSender>();
else
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

builder.Services.AddScoped<AlertService>();
builder.Services.AddHostedService<DailyAlertsJob>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});


var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

// În dezvoltare, frontend-ul ajunge la API prin proxy-ul Vite pe HTTP; o redirecționare
// spre HTTPS l-ar trimite pe alt port, iar browserul ar bloca cererea ca CORS.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
