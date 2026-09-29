using MiCarroAlDia.Infrastructure;
using MiCarroAlDia.Infrastructure.Persistence.Postgres;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Registrar Clean Architecture: Dominio, Aplicación e Infraestructura (Supabase PostgreSQL o InMemory)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Si se configuró la cadena de conexión a Supabase, inicializar tablas y sembrar datos
var supabaseConnectionString = app.Configuration.GetConnectionString("Supabase");
if (!string.IsNullOrWhiteSpace(supabaseConnectionString))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MiCarroAlDiaDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        await dbContext.Database.EnsureCreatedAsync();
        await PostgresSeeder.SeedAsync(dbContext, timeProvider);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "No se pudo inicializar la base de datos Supabase; continuando...");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// HttpsRedirection omitido en HTTP local

app.UseRouting();

app.UseAuthorization();

// Enlace corto tipo WhatsApp: /t/{token} -> /Tracking/{token}
app.MapGet("/t/{token}", (string token) => Results.Redirect($"/Tracking/{token}"));
app.MapGet("/Marcela", () => Results.Redirect("/Taller"));

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program { }
