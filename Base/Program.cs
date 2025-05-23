using Base.Data.Context;
using Base.Data.Seeding;
using Base.Models;
using Base.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Register the custom authorization policy provider
builder.Services.AddSingleton<IAuthorizationPolicyProvider, BypassAuthorizationPolicyProvider>();

// Add rate limiting services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<RateLimitingService>();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Validasyon filtresi ekle
    options.Filters.Add<ValidationFilter>();
    // ASP.NET Core'un varsayılan model doğrulama davranışını devre dışı bırak
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
})
.ConfigureApiBehaviorOptions(options =>
{
    // Varsayılan validasyon davranışını devre dışı bırak
    options.SuppressModelStateInvalidFilter = true;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "Base API", 
        Version = "v1",
        Description = "A simple ASP.NET Core Web API for managing devices and their logs.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Your Name/Company",
            Email = string.Empty,
            Url = new Uri("https://example.com/contact"),
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "Use under LICX",
            Url = new Uri("https://example.com/license"),
        }
    });
    
    // JWT için Authorization butonunu ekle
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Configure JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

var secretKey = jwtSettings["Secret"];
var key = Encoding.ASCII.GetBytes(secretKey);

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        ClockSkew = TimeSpan.Zero
    };
    
    // 401 Unauthorized hataları için özel yanıt formatını ayarla
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            // Varsayılan işlemi engelle
            context.HandleResponse();
            
            // ApiResponse formatında 401 yanıtı oluştur
            var response = ApiResponse<object>.Unauthorized(
                "Bu işlemi gerçekleştirmek için giriş yapmanız gerekmektedir");
            
            // Response'u JSON formatında ayarla
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            
            // JSON formatına çevirip yanıtı gönder
            await context.Response.WriteAsJsonAsync(response);
        },
        
        OnForbidden = async context =>
        {
            // ApiResponse formatında 403 yanıtı oluştur
            var response = ApiResponse<object>.Forbidden(
                "Bu işlemi gerçekleştirmek için yetkiniz bulunmamaktadır");
            
            // Response'u JSON formatında ayarla
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            
            // JSON formatına çevirip yanıtı gönder
            await context.Response.WriteAsJsonAsync(response);
        }
    };
});

// Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole(UserRole.User.ToString()));
    options.AddPolicy("RequireDeveloperRole", policy => policy.RequireRole(UserRole.Developer.ToString()));
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole(UserRole.Admin.ToString(), UserRole.SuperAdmin.ToString()));
    options.AddPolicy("RequireSuperAdminRole", policy => policy.RequireRole(UserRole.SuperAdmin.ToString()));
});

// Add database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetValue<string>("Database:ConnectionStrings:DefaultConnection"),
        sqlServerOptionsAction: sqlOptions => 
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

// Add database seeder
builder.Services.AddTransient<DatabaseSeeder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => 
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
        c.SerializeAsV2 = false;  // OpenAPI 3.0 formatında serialize et
    });
    
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Base API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Base API Documentation";
        c.DefaultModelsExpandDepth(1);
        c.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();

// Add rate limiting middleware before authentication/authorization
// This ensures rate limiting happens before any authentication logic
app.UseCustomRateLimiting();

// Get security bypass setting
var bypassSecurity = builder.Configuration.GetValue<bool>("Security:BypassSecurity");

// Always register authentication - our custom provider will handle bypassing
app.UseAuthentication();
app.UseAuthorization();

if (bypassSecurity)
{
    app.Logger.LogWarning("GÜVENLİK UYARISI: Kimlik doğrulama ve yetkilendirme kontrolleri devre dışı bırakılmıştır. Bu ayar sadece geliştirme ortamında kullanılmalıdır!");
}

app.MapControllers();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // Veritabanı yapılandırması
        var dropDbOnStartup = app.Configuration.GetValue<bool>("Database:Drop:Startup");
        if (dropDbOnStartup)
        {
            logger.LogInformation("Veritabanı drop edilecek...");
            await context.Database.EnsureDeletedAsync();
            logger.LogInformation("Veritabanı drop edildi.");
        }
        
        // Apply migrations
        logger.LogInformation("Veritabanı ve tablolar oluşturuluyor...");
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("Veritabanı ve tablolar oluşturuldu.");
        
        // Seed data - konfigürasyona göre koşullu olarak çalıştır
        var enableSeeding = app.Configuration.GetValue<bool>("Database:Seed:EnableSeeding");
        if (enableSeeding)
        {
            logger.LogInformation("Seed işlemi başlatılıyor...");
            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
            logger.LogInformation("Seed işlemi tamamlandı.");
        }
        else
        {
            logger.LogInformation("Seed işlemi yapılandırma dosyasında devre dışı bırakılmış.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı işlemlerinde bir hata oluştu.");
    }
}

app.Run();
