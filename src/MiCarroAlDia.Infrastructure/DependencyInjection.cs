using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiCarroAlDia.Application.Abstractions;
using MiCarroAlDia.Application.AdditionalResponses;
using MiCarroAlDia.Application.CustomerTracking;
using MiCarroAlDia.Infrastructure.Persistence.InMemory;
using MiCarroAlDia.Infrastructure.Persistence.Postgres;

namespace MiCarroAlDia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // Proveedor de tiempo por defecto
        services.AddSingleton(TimeProvider.System);

        var supabaseConnectionString = configuration?.GetConnectionString("Supabase") 
            ?? configuration?.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrWhiteSpace(supabaseConnectionString))
        {
            // Persistencia real en Supabase (PostgreSQL)
            services.AddDbContext<MiCarroAlDiaDbContext>(options =>
            {
                options.UseNpgsql(supabaseConnectionString);
            });

            services.AddScoped<IWorkshopRepository, PostgresWorkshopRepository>();
            services.AddScoped<IWorkOrderRepository, PostgresWorkOrderRepository>();
            services.AddScoped<IAdditionalQuoteRepository, PostgresAdditionalQuoteRepository>();
            services.AddScoped<ICustomerAccessLinkRepository, PostgresCustomerAccessLinkRepository>();
        }
        else
        {
            // Almacenamiento en memoria para modo local / prueba rápida
            services.AddSingleton<InMemoryDatabase>();

            services.AddScoped<IWorkshopRepository, InMemoryWorkshopRepository>();
            services.AddScoped<IWorkOrderRepository, InMemoryWorkOrderRepository>();
            services.AddScoped<IAdditionalQuoteRepository, InMemoryAdditionalQuoteRepository>();
            services.AddScoped<ICustomerAccessLinkRepository, InMemoryCustomerAccessLinkRepository>();
        }

        // Casos de uso de la aplicación
        services.AddScoped<GetCustomerTrackingUseCase>();
        services.AddScoped<SubmitCustomerResponseUseCase>();

        return services;
    }
}
