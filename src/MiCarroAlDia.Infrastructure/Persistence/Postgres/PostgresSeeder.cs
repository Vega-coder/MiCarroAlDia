using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MiCarroAlDia.Domain.Entities;
using MiCarroAlDia.Domain.Enums;

namespace MiCarroAlDia.Infrastructure.Persistence.Postgres;

public static class PostgresSeeder
{
    public static async Task SeedAsync(MiCarroAlDiaDbContext context, TimeProvider timeProvider)
    {
        try
        {
            var creator = (Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator)context.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator>();
            await creator.CreateTablesAsync();
        }
        catch
        {
            // Las tablas ya podrían existir
        }

        if (await context.Workshops.AnyAsync())
        {
            return; // Ya fue sembrado
        }

        var nowUtc = timeProvider.GetUtcNow();

        // 1. Talleres
        var workshop1 = new Workshop("taller-autofrenos", "Autofrenos del Norte", "+57 (604) 444-1234", "Calle 65 # 50-20, Barrio Sevilla", "Medellín");
        var workshop2 = new Workshop("taller-sur", "Serviteca y Frenos del Sur", "+57 (604) 333-9876", "Carrera 43A # 25-10, El Poblado", "Medellín");
        context.Workshops.AddRange(workshop1, workshop2);

        // 2. Escenario 1: Cotización PENDIENTE (Renault Sandero)
        var order1 = new WorkOrder("OT-101", workshop1.Id, "ABC-123", "Renault Sandero Dynamique 1.6", "Carlos Mario Gómez", VehicleProgressState.Recibido, nowUtc.AddHours(-24), "Vehículo recibido para mantenimiento y revisión de frenos.");
        order1.AdvanceProgress(VehicleProgressState.Diagnostico, nowUtc.AddHours(-20), "Inspección detallada: se detecta desgaste severo en pastillas y discos de freno.");
        context.WorkOrders.Add(order1);

        var quote1 = new AdditionalQuote(
            "COT-101",
            order1.Id,
            nowUtc.AddHours(-20),
            new[]
            {
                new QuoteItem("ITM-101-1", "Juego de pastillas de freno delanteras (Cerámica de alto rendimiento)", ItemType.Repuesto, ItemCategory.Seguridad, 1, 200_000m),
                new QuoteItem("ITM-101-2", "Mano de obra: desmontaje, rectificación de discos e instalación de frenos", ItemType.ManoDeObra, ItemCategory.Seguridad, 1, 100_000m),
                new QuoteItem("ITM-101-3", "Juego de plumillas limpiaparabrisas siliconadas Bosch", ItemType.Repuesto, ItemCategory.General, 1, 50_000m)
            }
        );
        context.Quotes.Add(quote1);

        var link1 = new CustomerAccessLink("demo-activa", workshop1.Id, order1.Id, nowUtc.AddHours(-20));
        context.AccessLinks.Add(link1);

        // 3. Escenario 2: Cotización RESPONDIDA (Chevrolet Onix)
        var order2 = new WorkOrder("OT-102", workshop1.Id, "XYZ-789", "Chevrolet Onix Turbo LTZ", "María Paula Restrepo", VehicleProgressState.Recibido, nowUtc.AddHours(-40), "Ingreso por ruido en suspensión delantera.");
        order2.AdvanceProgress(VehicleProgressState.Diagnostico, nowUtc.AddHours(-36), "Diagnóstico confirma fuga de aceite en amortiguadores delanteros.");
        order2.AdvanceProgress(VehicleProgressState.Reparacion, nowUtc.AddHours(-18), "Repuestos recibidos en taller; técnico asignado a ensamble.");
        context.WorkOrders.Add(order2);

        var quote2 = new AdditionalQuote(
            "COT-102",
            order2.Id,
            nowUtc.AddHours(-30),
            new[]
            {
                new QuoteItem("ITM-102-1", "Par de amortiguadores delanteros a gas", ItemType.Repuesto, ItemCategory.Seguridad, 1, 380_000m),
                new QuoteItem("ITM-102-2", "Mano de obra montaje y desmontaje de amortiguadores", ItemType.ManoDeObra, ItemCategory.Seguridad, 1, 120_000m),
                new QuoteItem("ITM-102-3", "Alineación y balanceo computarizado de 4 ruedas", ItemType.ManoDeObra, ItemCategory.General, 1, 60_000m)
            }
        );
        var itemResponses2 = new List<ItemResponse>
        {
            new("ITM-102-1", "Par de amortiguadores delanteros a gas", ItemType.Repuesto, ItemCategory.Seguridad, CustomerDecision.Aprobar, false, 1, 380_000m, 380_000m, 72_200m, 452_200m),
            new("ITM-102-2", "Mano de obra montaje y desmontaje de amortiguadores", ItemType.ManoDeObra, ItemCategory.Seguridad, CustomerDecision.Aprobar, false, 1, 120_000m, 120_000m, 22_800m, 142_800m),
            new("ITM-102-3", "Alineación y balanceo computarizado de 4 ruedas", ItemType.ManoDeObra, ItemCategory.General, CustomerDecision.Rechazar, false, 1, 60_000m, 60_000m, 11_400m, 71_400m)
        };
        quote2.SetSeededResponse(new QuoteResponse("RESP-102", nowUtc.AddHours(-19), itemResponses2));
        context.Quotes.Add(quote2);

        var link2 = new CustomerAccessLink("demo-respondida", workshop1.Id, order2.Id, nowUtc.AddHours(-30));
        context.AccessLinks.Add(link2);

        // 4. Escenario 3: Cotización VENCIDA (Toyota Hilux)
        var order3 = new WorkOrder("OT-103", workshop1.Id, "KLR-456", "Toyota Hilux 2.8 4x4", "Héctor Fabio Ramírez", VehicleProgressState.Recibido, nowUtc.AddHours(-72), "Revisión de frenos de disco traseros.");
        order3.AdvanceProgress(VehicleProgressState.Diagnostico, nowUtc.AddHours(-60), "Se identificaron bandas traseras desgastadas.");
        context.WorkOrders.Add(order3);

        var quote3 = new AdditionalQuote(
            "COT-103",
            order3.Id,
            nowUtc.AddHours(-55),
            new[]
            {
                new QuoteItem("ITM-103-1", "Kit de bandas de freno traseras", ItemType.Repuesto, ItemCategory.Seguridad, 1, 240_000m),
                new QuoteItem("ITM-103-2", "Mano de obra graduación y cambio de bandas", ItemType.ManoDeObra, ItemCategory.Seguridad, 1, 90_000m)
            }
        );
        context.Quotes.Add(quote3);

        var link3 = new CustomerAccessLink("demo-vencida", workshop1.Id, order3.Id, nowUtc.AddHours(-55));
        context.AccessLinks.Add(link3);

        // 5. Escenario 4: Orden SIN ADICIONALES (Kia Picanto)
        var order4 = new WorkOrder("OT-104", workshop1.Id, "MNO-321", "Kia Picanto Ion 1.25", "Luisa Fernanda Duque", VehicleProgressState.Recibido, nowUtc.AddHours(-15), "Mantenimiento básico de 30.000 km.");
        order4.AdvanceProgress(VehicleProgressState.Diagnostico, nowUtc.AddHours(-12), "Todo en óptimo estado; no se detectaron daños adicionales.");
        order4.AdvanceProgress(VehicleProgressState.Reparacion, nowUtc.AddHours(-8), "Cambio de aceite y filtros regulares ejecutados.");
        order4.AdvanceProgress(VehicleProgressState.ControlDeCalidad, nowUtc.AddHours(-3), "Inspección de 25 puntos aprobada con éxito.");
        order4.AdvanceProgress(VehicleProgressState.ListoParaEntregar, nowUtc.AddHours(-1), "Vehículo lavado e higienizado listo para entrega.");
        context.WorkOrders.Add(order4);

        var link4 = new CustomerAccessLink("demo-sin-adicionales", workshop1.Id, order4.Id, nowUtc.AddHours(-15));
        context.AccessLinks.Add(link4);

        // 6. Escenario 5: Serviteca del Sur (Multitenant)
        var order5 = new WorkOrder("OT-201", workshop2.Id, "SUR-555", "Nissan Versa Advance", "Andrés Felipe Uribe", VehicleProgressState.Recibido, nowUtc.AddHours(-10), "Ingreso a Serviteca y Frenos del Sur para alineación.");
        context.WorkOrders.Add(order5);

        var link5 = new CustomerAccessLink("demo-taller-sur", workshop2.Id, order5.Id, nowUtc.AddHours(-10));
        context.AccessLinks.Add(link5);

        await context.SaveChangesAsync();
    }
}
