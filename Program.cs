using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;

var builder = WebApplication.CreateBuilder(args);

// Добавление DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Добавление сервисов MVC (обязательно!)
builder.Services.AddControllersWithViews();

// Добавление контроллеров API (если есть)
builder.Services.AddControllers();

var app = builder.Build();

// Статические файлы (CSS, JS, изображения)
app.UseStaticFiles();

// Маршрутизация
app.UseRouting();

// Маппинг контроллеров API
app.MapControllers();

// Маппинг MVC маршрутов
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();