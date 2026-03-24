using BusinessLayer.Interfaces;
using BusinessLayer.Services;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURARE BAZĂ DE DATE
// ==========================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("DataAccessLayer")));

// ==========================================
// 2. CONFIGURARE IDENTITY
// ==========================================
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ==========================================
// 3. CONFIGURARE JWT (Token Security)
// ==========================================
var secretKey = builder.Configuration["JwtConfig:Secret"];
if (string.IsNullOrEmpty(secretKey))
{
    throw new Exception("Cheia secretă JWT lipsește din appsettings.json!");
}

var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(jwt => {
    jwt.RequireHttpsMetadata = false;
    jwt.SaveToken = true;
    jwt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,   // Simplificat pentru dev
        ValidateAudience = false, // Simplificat pentru dev
        ValidateLifetime = true,  // Verifică dacă token-ul a expirat
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = "Role"    // Caută permisiunile în câmpul "Role" din token
    };
    jwt.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("jwt"))
            {
                context.Token = context.Request.Cookies["jwt"];

            }
            return Task.CompletedTask;
        }
    };
});

// ==========================================
// 4. INJECTARE SERVICII (DI)
// ==========================================

// MAGIC: Ne ajută să extragem InstitutionId din token-ul userului curent logat
builder.Services.AddHttpContextAccessor();

// Injectarea serviciilor standard
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICitizenDocumentService, CitizenDocumentService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IOfficialDocumentService, OfficialDocumentService>();
builder.Services.AddScoped<ICitizenDocumentResponseService, CitizenDocumentResponseService>();

// Injectarea noului serviciu de SysAdmin creat mai devreme
builder.Services.AddScoped<ISysAdminService, SysAdminService>();
builder.Services.AddScoped<IDocumentTypesService, DocumentTypesService>();
// Worker pentru curățarea token-urilor expirate
builder.Services.AddHostedService<TokenCleanupWorker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ==========================================
// 5. CONFIGURARE SWAGGER (Cu buton de Login)
// ==========================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Disertatie API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Introdu token-ul JWT astfel: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("LoginLimit", httpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
         partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
         factory: partition => new FixedWindowRateLimiterOptions
         {
             PermitLimit = 5,
             Window = TimeSpan.FromMinutes(1),
             QueueLimit = 0,
             AutoReplenishment=true
         }
        ));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:5173") // Portul de la Vite/React
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // ESENȚIAL PENTRU COOKIES!
    });
});

var app = builder.Build();

// ==========================================
// 6. SEEDING AUTOMAT (Apelează noua clasă)
// ==========================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        // Apelăm metoda curată din clasa DbSeeder
        await DataAccessLayer.Data.DbSeeder.SeedDataAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> CRITICAL ERROR AT SEEDING: {ex.Message} <<<");
    }
}

// ==========================================
// 7. PIPELINE HTTP
// ==========================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication(); // 1. Cine ești?
app.UseAuthorization();  // 2. Ce ai voie să faci?

app.MapControllers();

app.Run();

public partial class Program { }