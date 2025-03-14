using DataAccessLayer.DataContext;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Contracts;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using PresentationLayer.Hubs;
using PresentationLayer.ErrorDescriber;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Razor;
using PresentationLayer.Utilities;
using Microsoft.AspNetCore.Components.Forms;
using PresentationLayer;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add Google Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration.GetSection("GoogleKeys:ClientId").Value;
    options.ClientSecret = builder.Configuration.GetSection("GoogleKeys:ClientSecret").Value;
    options.SaveTokens = true;
});


// Добавление CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost/")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

//Добавление локализации
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new List<CultureInfo>
    {
        new CultureInfo("en-US"),
        new CultureInfo("ru-RU"),
        new CultureInfo("zh-CN")
    };

    options.DefaultRequestCulture = new RequestCulture("ru");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});
builder.Services.AddSingleton<SharedViewLocalizer>();

builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

// Настройка кэша
builder.Services.AddDistributedMemoryCache();

// Настройка сессий
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});



builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});


// Добавление сервисов в контейнер
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<DataStoreDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Sql"));
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Sql"));
});

builder.Services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Directory.GetCurrentDirectory()));

builder.Services.AddTransient<IUserRepository, UserRepository>();
builder.Services.AddTransient<IQuizRepository, QuizRepository>();
builder.Services.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddTransient<IAdminTokenRepository, AdminTokenRepository>();

builder.Services.AddTransient<IImageService>(provider =>
    new ImageService(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/default-profile-pictures"))); 
builder.Services.AddScoped<IQuestionsService, QuestionsService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IAdminTokenService, AdminTokenService>();
builder.Services.AddScoped<CultureHelper>();

builder.Services.AddSignalR();

builder.Services.AddHttpContextAccessor();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequiredUniqueChars = 0;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;

    // Добавляем провайдер токенов для 2FA (Google Authenticator)
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

// Configure cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // Длительность сеанса при RememberMe
    options.SlidingExpiration = true;
    options.LoginPath = "/Home/Login";
    options.LogoutPath = "/Home/Logout";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Регистрация LocalizedIdentityErrorDescriber
builder.Services.AddScoped<LocalizedIdentityErrorDescriber>();

// Добавление защиты данных
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\temp\keys\"))
    .SetApplicationName("Quiz")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(14));


var app = builder.Build();

app.UseCors();

// Настройка локализатора
var locOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>();

if (locOptions != null)
{
    app.UseRequestLocalization(locOptions.Value);
}

// Настройка конвейера обработки HTTP-запросов
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
});
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.MapHub<GameHub>("/gamehub");

// Инициализация ролей при запуске
using (var scope = app.Services.CreateScope())
{
    var service = scope.ServiceProvider;
    try
    {
        var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = service.GetRequiredService<UserManager<User>>();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
    }
    catch (Exception ex)
    {
        // Логирование ошибок
        var logger = service.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка инициализации ролей");
    }
}

app.Run();

async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
{
    // Определяем список ролей, которые нам нужны
    string[] roles = new string[] { "Admin", "User" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

async Task SeedAdminUserAsync(UserManager<User> userManager)
{

    // Создаем админа, если его нет (замените данные на нужные)
    string adminEmail = "admin@quizik.fun";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            Name = "Администратор",
            CreatedAt = DateTime.Now
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123$"); // Укажите надежный пароль

        if (result.Succeeded)
        {
            // Добавляем роль "Admin" для этого пользователя
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
public partial class Program { }
