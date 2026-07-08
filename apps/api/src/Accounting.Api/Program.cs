using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Accounting.Api.Filters;
using Accounting.Api.Middleware;
using Accounting.Api.Services;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Accounting.Application.Validators;
using Accounting.Infrastructure.Persistence;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Rate limiting — 10 requests/min per IP on auth endpoints
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                Window            = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                PermitLimit       = 10,
                QueueLimit        = 0,
            }));
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            """{"title":"Demasiados intentos. Espera un momento antes de intentar de nuevo."}""", ct);
    };
});

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();
builder.Services.AddScoped<IJournalRepository, JournalRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
builder.Services.AddScoped<IMemberInvitationRepository, MemberInvitationRepository>();
builder.Services.AddScoped<IOrganizationSettingsRepository, OrganizationSettingsRepository>();
builder.Services.AddScoped<IAccountingPeriodRepository, AccountingPeriodRepository>();
builder.Services.AddScoped<IYearEndClosingRepository, YearEndClosingRepository>();
builder.Services.AddScoped<IRecurringEntryRepository, RecurringEntryRepository>();
builder.Services.AddScoped<IBankRepository, BankRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IBudgetRepository, BudgetRepository>();

// Validators
builder.Services.AddScoped<IValidator<RegisterDto>, RegisterDtoValidator>();
builder.Services.AddScoped<IValidator<LoginDto>, LoginDtoValidator>();
builder.Services.AddScoped<IValidator<DeleteAccountDto>, DeleteAccountDtoValidator>();
builder.Services.AddScoped<IValidator<ChangePasswordDto>, ChangePasswordDtoValidator>();
builder.Services.AddScoped<IValidator<ForgotPasswordDto>, ForgotPasswordDtoValidator>();
builder.Services.AddScoped<IValidator<ResetPasswordDto>, ResetPasswordDtoValidator>();
builder.Services.AddScoped<IValidator<VerifyEmailDto>, VerifyEmailDtoValidator>();
builder.Services.AddScoped<IValidator<CreateAccountDto>, CreateAccountDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateAccountDto>, UpdateAccountDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateProfileDto>, UpdateProfileDtoValidator>();
builder.Services.AddScoped<IValidator<CreateJournalEntryDto>, CreateJournalEntryDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateJournalEntryDto>, UpdateJournalEntryDtoValidator>();
builder.Services.AddScoped<IValidator<VoidJournalEntryDto>, VoidJournalEntryDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateOrgSettingsDto>, UpdateOrgSettingsDtoValidator>();
builder.Services.AddScoped<IValidator<InviteMemberDto>, InviteMemberDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateMemberRoleDto>, UpdateMemberRoleDtoValidator>();
builder.Services.AddScoped<IValidator<ClosePeriodDto>, ClosePeriodDtoValidator>();
builder.Services.AddScoped<IValidator<YearEndCloseRequestDto>, YearEndCloseRequestDtoValidator>();
builder.Services.AddScoped<IValidator<CreateRecurringEntryDto>, CreateRecurringEntryDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateRecurringEntryDto>, UpdateRecurringEntryDtoValidator>();
builder.Services.AddScoped<IValidator<CreateBankAccountDto>, CreateBankAccountDtoValidator>();
builder.Services.AddScoped<IValidator<ImportBankTransactionDto>, ImportBankTransactionDtoValidator>();
builder.Services.AddScoped<IValidator<CreateContactDto>, CreateContactDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateContactDto>, UpdateContactDtoValidator>();
builder.Services.AddScoped<IValidator<CreateInvoiceDto>, CreateInvoiceDtoValidator>();
builder.Services.AddScoped<IValidator<CreatePaymentDto>, CreatePaymentDtoValidator>();
builder.Services.AddScoped<IValidator<CreateBudgetDto>, CreateBudgetDtoValidator>();
builder.Services.AddScoped<IValidator<UpsertBudgetLineDto>, UpsertBudgetLineDtoValidator>();
builder.Services.AddScoped<IValidator<CreateTaxRateDto>, CreateTaxRateDtoValidator>();
builder.Services.AddScoped<IValidator<CreateProductDto>, CreateProductDtoValidator>();

// Filters
builder.Services.AddScoped<OrgMembershipFilter>();

// Services
builder.Services.AddSingleton(new Accounting.Application.Services.AuthSettings
{
    RefreshExpiryDays = int.Parse(builder.Configuration["Jwt:RefreshExpiryDays"] ?? "7"),
});
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAccountSeeder, AccountSeeder>();
builder.Services.AddScoped<IJournalService, JournalService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IOrgSettingsService, OrgSettingsService>();
builder.Services.AddScoped<IExportService, Accounting.Infrastructure.Export.ExportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPeriodService, PeriodService>();
builder.Services.AddScoped<IYearEndClosingService, YearEndClosingService>();
builder.Services.AddScoped<IRecurringEntryService, RecurringEntryService>();
builder.Services.AddScoped<IBankService, BankService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<ITaxRateRepository, TaxRateRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ITaxRateService, TaxRateService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOpeningBalanceService, OpeningBalanceService>();

// Email notification service
var emailSettings = new EmailServiceSettings
{
    BaseUrl = builder.Configuration["EmailService:BaseUrl"] ?? "",
    Secret  = builder.Configuration["EmailService:Secret"]  ?? "",
    AppUrl  = builder.Configuration["EmailService:AppUrl"]  ?? "http://localhost:3000",
};
builder.Services.AddSingleton(emailSettings);
builder.Services.AddHttpClient<IEmailNotificationService, Accounting.Infrastructure.Email.HttpEmailNotificationService>(client =>
{
    if (!string.IsNullOrEmpty(emailSettings.BaseUrl))
        client.BaseAddress = new Uri(emailSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.MapInboundClaims = false; // Keep JWT claim names as-is (e.g. "sub" stays "sub")
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddPolicy("web", p =>
    p.WithOrigins(builder.Configuration["Cors:WebOrigin"] ?? "http://localhost:3000")
     .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
