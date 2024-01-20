using System.Text;
using Carter;
using EasyNetQ;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TNG.Users.API.BackgroundServices;
using TNG.Users.API.Common.Behaviours;
using TNG.Users.API.Common.Configuration;
using TNG.Users.API.Common.Services;
using TNG.Users.API.Common.Services.Impl;
using TNG.Users.API.Database;
using TNG.Users.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);
var thisAssembly = typeof(Program).Assembly;
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(swaggerOptions =>
{
    const string securityScheme = "Bearer";
    swaggerOptions.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Users",
    });

    swaggerOptions.AddSecurityDefinition(securityScheme, new OpenApiSecurityScheme
    {
        Description = "Standard authorisation using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = securityScheme,
        BearerFormat = "JWT",
    });

    swaggerOptions.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = securityScheme
                }
            },
            Array.Empty<string>()
        }
    });
    swaggerOptions.CustomSchemaIds(type => type.FullName?.Replace("+", "."));;
});
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(thisAssembly));
builder.Services.AddDbContext<UserDbContext>(optBuilder => optBuilder.UseNpgsql(builder.Configuration.GetConnectionString("AppDatabase")));
builder.Services.AddValidatorsFromAssembly(thisAssembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.RegisterEasyNetQ((_) => new ConnectionConfiguration(){Hosts = [new HostConfiguration()
{
    Host = "localhost",
    Port = 5672,
}]});
builder.Services.AddHostedService<NotificationBackgroundService>();
var jwtOptions = builder.Configuration
    .GetRequiredSection("Jwt")
    .Get<JwtOptions>(binderOptions => binderOptions.BindNonPublicProperties = true);
builder.Services.AddSingleton(jwtOptions!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(authBuilder =>
{
    var signingKeyBytes = Encoding.UTF8.GetBytes(jwtOptions!.SigningKey);

    authBuilder.SaveToken = true;
    authBuilder.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddTransient<ITokenProvider, JwtTokenProvider>();
builder.Services.AddCarter();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionHandler>();
app.UseAuthentication();
app.UseAuthorization();
app.MapCarter();

app.Run();
