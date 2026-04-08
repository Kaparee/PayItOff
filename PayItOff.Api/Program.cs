using Azure.Core;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using PayItOff.Api.Middleware;
using PayItOff.Application.Interfaces;
using PayItOff.Application.Services;
using PayItOff.Application.Validators;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;
using PayItOff.Infrastructure.Repositories;
using PayItOff.Shared.Requests;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Rejestracja PayItOffDbContext
builder.Services.AddDbContext<PayItOffDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJWTService, JWTService>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // To odblokuje adres /swagger
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
