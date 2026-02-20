using BusinessLayer.Interfaces;
using BusinessLayer.Services;
using DataAccessLayer.Data;
using DataAccessLayer.Entities; // Asigură-te că UserRole este aici
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

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
// Validare cheie secretă (să nu fie null)
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
        ClockSkew = TimeSpan.Zero, // Fără toleranță la expirare

        // FOARTE IMPORTANT: Aici spunem sistemului să caute permisiunile în câmpul "Role"
        RoleClaimType = "Role"
    };
});

// ==========================================
// 4. INJECTARE SERVICII (DI)
// ==========================================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICitizenDocumentService, CitizenDocumentService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IOfficialDocumentService, OfficialDocumentService>();
builder.Services.AddScoped<ICitizenDocumentResponseService, CitizenDocumentResponseService>();

// Worker pentru curățarea token-urilor expirate (background task)
builder.Services.AddHostedService<TokenCleanupWorker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ==========================================
// 5. CONFIGURARE SWAGGER (Cu buton de Login)
// ==========================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Disertatie API", Version = "v1" });

    // Definiția securității (Lacătul)
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

var app = builder.Build();

// ==========================================
// 6. SEEDING AUTOMAT (Creare Admin)
// ==========================================
// Acest bloc rulează la fiecare pornire și verifică dacă există Admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        
        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        // ------------------------------------------------------------
        // PARTEA NOUĂ: SEED PENTRU DEPARTAMENTE (CompetencyProfiles)
        // ------------------------------------------------------------
        if (!await context.CompetencyProfiles.AnyAsync())
        {
            Console.WriteLine(">>> Seeding Departamente...");

            var departamente = new List<CompetencyProfile>
            {
                new CompetencyProfile
                { 
                    // ID FIX: 1111... (Ușor de ținut minte)
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Urbanism",
                    Description = "Emitere certificate urbanism și autorizații."
                },
                new CompetencyProfile
                { 
                    // ID FIX: 2222...
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Taxe și Impozite",
                    Description = "Colectare taxe locale și amenzi."
                },
                new CompetencyProfile
                { 
                    // ID FIX: 3333...
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Stare Civilă",
                    Description = "Evidența populației, căsătorii, nașteri."
                }
            };

            await context.CompetencyProfiles.AddRangeAsync(departamente);
            await context.SaveChangesAsync();
            Console.WriteLine(">>> Departamente create cu succes!");
        }

        var adminEmail = "admin@local.com"; // Email-ul de Admin

        var adminExists = await userManager.FindByEmailAsync(adminEmail);

        if (adminExists == null)
        {
            var newAdmin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Super Administrator",
                // AICI E ESENȚIAL: Setăm rolul direct din Enum
                Role = UserRole.Admin
            };

            // Parola trebuie să fie complexă (Litere mari, mici, cifre, simboluri)
            var result = await userManager.CreateAsync(newAdmin, "AdminPass123!");

            if (result.Succeeded)
            {
                Console.WriteLine(">>> ADMIN CREAT CU SUCCES: admin@local.com / AdminPass123! <<<");
            }
            else
            {
                Console.WriteLine($">>> EROARE LA CREARE ADMIN: {string.Join(", ", result.Errors.Select(e => e.Description))} <<<");
            }
        }
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

app.UseHttpsRedirection();

// Ordinea este critică aici:
app.UseAuthentication(); // 1. Cine ești?
app.UseAuthorization();  // 2. Ce ai voie să faci?

app.MapControllers();

app.Run();