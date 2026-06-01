using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PayItOff.Api.Middleware;
using PayItOff.Application.Interfaces;
using PayItOff.Application.Services;
using PayItOff.Application.Validators;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Authentication;
using PayItOff.Infrastructure.Persistence;
using PayItOff.Infrastructure.Repositories;
using PayItOff.Infrastructure.Services;
using PayItOff.Shared.Requests;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Rejestracja PayItOffDbContext i Interceptorów
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PayItOff.Infrastructure.Persistence.Interceptors.AuditLogInterceptor>();

builder.Services.AddDbContext<PayItOffDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString)
           .AddInterceptors(sp.GetRequiredService<PayItOff.Infrastructure.Persistence.Interceptors.AuditLogInterceptor>());
});

// KONFIGURACJA JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateLifetime = true
        };
    });

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PayItOff.Api", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Sam token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

// Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IGroupMemberRepository, GroupMemberRepository>();
builder.Services.AddScoped<IFriendRepository, FriendRepository>();
builder.Services.AddScoped<IGroupDebtRepository, GroupDebtRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<IExpenseSplitRepository, ExpenseSplitRepository>();
builder.Services.AddScoped<ISettlementRepository, SettlementRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJWTService, JWTService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IGroupMemberService, GroupMemberService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISettlementService, SettlementService>();

// Validators
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<IValidator<CreateGroupRequest>, CreateGroupRequestValidator>();

builder.Services.AddScoped<IDailySummaryJob, DailySummaryJob>();

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));
    
builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PayItOffDbContext>();
    try
    {
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Błąd migracji: {ex.Message}");
    }
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard();

RecurringJob.AddOrUpdate<IDailySummaryJob>(
    "daily-summary-job",
    job => job.ExecuteAsync(),
    "0 20 * * *"
);

app.MapControllers();

app.Run();