using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RFQAPI.DAL.DbConnection;
using RFQAPI.Helpers;
using RFQAPI.Middleware;
using RFQAPI.Repositories.Implementations;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Implementations;
using RFQAPI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are missing.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
{
    throw new InvalidOperationException(
        "JWT signing key is missing or too short. Set Jwt:Key via User Secrets or environment variables (minimum 32 characters).");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<DapperContext>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IRfqRepository, RfqRepository>();
builder.Services.AddScoped<IRfqRoundRepository, RfqRoundRepository>();
builder.Services.AddScoped<IBidRepository, BidRepository>();
builder.Services.AddScoped<IEvaluationRepository, EvaluationRepository>();
builder.Services.AddScoped<IEvaluationDecisionRepository, EvaluationDecisionRepository>();
builder.Services.AddScoped<IAwardRepository, AwardRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IRfqService, RfqService>();
builder.Services.AddScoped<IRfqRoundService, RfqRoundService>();
builder.Services.AddScoped<IBidService, BidService>();
builder.Services.AddScoped<IEvaluationService, EvaluationService>();
builder.Services.AddScoped<IEvaluationDecisionService, EvaluationDecisionService>();
builder.Services.AddScoped<IAwardService, AwardService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddSingleton<JwtTokenHelper>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ABB RFQ Portal API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();