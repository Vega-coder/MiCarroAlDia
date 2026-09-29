using System.Collections.Concurrent;
using MiCarroAlDia.Domain.Entities;
using MiCarroAlDia.Domain.Enums;

namespace MiCarroAlDia.Infrastructure.Persistence.InMemory;

public class InMemoryDatabase
{
    private readonly object _lock = new();

    public ConcurrentDictionary<string, Workshop> Workshops { get; } = new();
    public ConcurrentDictionary<string, WorkOrder> WorkOrders { get; } = new();
    public ConcurrentDictionary<string, AdditionalQuote> Quotes { get; } = new();
    public ConcurrentDictionary<string, CustomerAccessLink> AccessLinks { get; } = new();

    public InMemoryDatabase(TimeProvider? timeProvider = null)
    {
        Seed(timeProvider ?? TimeProvider.System);
    }

    public void Seed(TimeProvider timeProvider)
    {
        var nowUtc = timeProvider.GetUtcNow();

        // 1. Talleres
        var workshop1 = new Workshop(
            id: "taller-autofrenos",
            name: "Autofrenos del Norte",
            phone: "+57 (604) 444-1234",
            address: "Calle 65 # 50-20, Barrio Sevilla",
            city: "Medellín"
        );
        Workshops[workshop1.Id] = workshop1;

        var workshop2 = new Workshop(
            id: "taller-sur",
            name: "Serviteca y Frenos del Sur",
            phone: "+57 (604) 333-9876",
            address: "Carrera 43A # 25-10, El Poblado",
            city: "Medellín"
        );
        Workshops[workshop2.Id] = workshop2;

        // 2. Escenario 1: Cotización PENDIENTE con ítems de seguridad y general (20 horas de antigüedad, restan 28h)
        var order1 = new WorkOrder(
            id: "OT-101",
            workshopId: workshop1.Id,
            vehiclePlate: "ABC-123",
            vehicleModel: "Renault Sandero Dynamique 1.6",
            customerName: "Carlos Mario Gómez",
            initialProgressState: VehicleProgressState.Recibido,
            createdAtUtc: nowUtc.AddHours(-24),
            initialNote: "Vehículo recibido para mantenimiento preventivo y revisión de frenos."
        );
        order1.AdvanceProgress(
            VehicleProgressState.Diagnostico,
            nowUtc.AddHours(-20),
            "Inspección detallada en elevador: se detecta desgaste severo en pastillas y discos de freno."
        );
        WorkOrders[order1.Id] = order1;

        var quote1 = new AdditionalQuote(
            id: "COT-101",
            workOrderId: order1.Id,
            publishedAtUtc: nowUtc.AddHours(-20), // 20h publicadas, vigencia 48h (quedan 28h)
            items: new[]
            {
                new QuoteItem(
                    id: "ITM-101-1",
                    description: "Juego de pastillas de freno delanteras (Cerámica de alto rendimiento)",
                    type: ItemType.Repuesto,
                    category: ItemCategory.Seguridad,
                    quantity: 1,
                    unitPrice: 200_000m
                ),
                new QuoteItem(
                    id: "ITM-101-2",
                    description: "Mano de obra: desmontaje, rectificación de discos e instalación de frenos",
                    type: ItemType.ManoDeObra,
                    category: ItemCategory.Seguridad,
                    quantity: 1,
                    unitPrice: 100_000m
                ),
                new QuoteItem(
                    id: "ITM-101-3",
                    description: "Juego de plumillas limpiaparabrisas siliconadas Bosch",
                    type: ItemType.Repuesto,
                    category: ItemCategory.General,
                    quantity: 1,
                    unitPrice: 50_000m
                )
            }
        );
        Quotes[quote1.Id] = quote1;

        var link1 = new CustomerAccessLink(
            token: "demo-activa",
            workshopId: workshop1.Id,
            workOrderId: order1.Id,
            createdAtUtc: nowUtc.AddHours(-20)
        );
        AccessLinks[link1.Token] = link1;

        // 3. Escenario 2: Cotización RESPONDIDA (Comprobante bloqueado)
        var order2 = new WorkOrder(
            id: "OT-102",
            workshopId: workshop1.Id,
            vehiclePlate: "XYZ-789",
            vehicleModel: "Chevrolet Onix Turbo LTZ",
            customerName: "María Paula Restrepo",
            initialProgressState: VehicleProgressState.Recibido,
            createdAtUtc: nowUtc.AddHours(-40),
            initialNote: "Ingreso por ruido en suspensión delantera al pasar por resaltos."
        );
        order2.AdvanceProgress(VehicleProgressState.Diagnostico, nowUtc.AddHours(-36), "Diagnóstico confirma fuga de aceite en amortiguadores delanteros.");
        order2.AdvanceProgress(VehicleProgressState.Reparacion, nowUtc.AddHours(-18), "Repuestos recibidos en taller; técnico asignado a ensamble.");
        WorkOrders[order2.Id] = order2;

        var quote2 = new AdditionalQuote(
            id: "COT-102",
            workOrderId: order2.Id,
            publishedAtUtc: nowUtc.AddHours(-30),
            items: new[]
            {
                new QuoteItem("ITM-102-1", "Par de amortiguadores delanteros a gas", ItemType.Repuesto, ItemCategory.Seguridad, 1, 380_000m),
                new QuoteItem("ITM-102-2", "Mano de obra montaje y desmontaje de amortiguadores", ItemType.ManoDeObra, ItemCategory.Seguridad, 1, 120_000m),
                new QuoteItem("ITM-102-3", "Alineación y balanceo computarizado de 4 ruedas", ItemType.ManoDeObra, ItemCategory.General, 1, 60_000m)
            }
        );

        // Sembrar respuesta registrada previamente
        var itemResponses2 = new List<ItemResponse>
        {
            new("ITM-102-1", "Par de amortiguadores delanteros a gas", ItemType.Repuesto, ItemCategory.Seguridad, CustomerDecision.Aprobar, false, 1, 380_000m, 380_000m, 72_200m, 452_200m),
            new("ITM-102-2", "Mano de obra montaje y desmontaje de amortiguadores", ItemType.ManoDeObra, ItemCategory.Seguridad, CustomerDecision.Aprobar, false, 1, 120_000m, 120_000m, 22_800m, 142_800m),
            new("ITM-102-3", "Alineación y balanceo computarizado de 4 ruedas", ItemType.ManoDeObra, ItemCategory.General, CustomerDecision.Rechazar, false, 1, 60_000m, 60_000m, 11_400m, 71_400m)
        };
        quote2.SetSeededResponse(new QuoteResponse("RESP-102", nowUtc.AddHours(-19), itemResponses2));
        Quotes[quote2.Id] = quote2;

        var link2 = new CustomerAccessLink(
            token: "demo-respondida",
            workshopId: workshop1.Id,
            workOrderId: order2.Id,
            createdAtUtc: nowUtc.AddHours(-30)
        );
        AccessLinks[link2.Token] = link2;

        // 4. Escenario 3: Cotización VENCIDA (> 48 horas continuas)
        var order3 = new WorkOrder(
            id: "OT-103",
            workshopId: workshop1.Id,
            vehiclePlate: "KLR-456",
            vehicleModel: "Toyota Hilux 2.8 4x4",
            customerName: "Héctor Fabio Ramírez",
            initialProgressState: VehicleProgressState.Recibido,
            createdAtUtc: nowUtc.AddHours(-72),
            initialNote: "Ingreso para revisión de frenos de disco traseros."
        );
        order3.AdvanceProgress(VehicleProgressState.Diagnostico, nowUtc.AddHours(-60), "Se identificaron bandas traseras desgastadas.");
        WorkOrders[order3.Id] = order3;

        var quote3 = new AdditionalQuote(
            id: "COT-103",
            workOrderId: order3.Id,
            publishedAtUtc: nowUtc.AddHours(-55), // 55h publicadas > 48h (Vencida)
            items: new[]
            {
                new QuoteItem("ITM-103-1", "Kit de bandas de freno traseras", ItemType.Repuesto, ItemCategory.Seguridad, 1, 240_000m),
                new QuoteItem("ITM-103-2", "Mano de obra graduación y cambio de bandas", ItemType.ManoDeObra, ItemCategory.Seguridad, 1, 90_000m)
            }
        );
        Quotes[quote3.Id] = quote3;

        var link3 = new CustomerAccessLink(
            token: "demo-vencida",
            workshopId: workshop1.Id,
            workOrderId: order3.Id,
            createdAtUtc: nowUtc.AddHours(-55)
        );
        AccessLinks[link3.Token] = link3;

        // 5. Escenario 4: Orden SIN ADICIONALES (Estado vacío en adicionales, vehículo listo)
        var order4 = new WorkOrder(
            id: "OT-104",
            workshopId: workshop1.Id,
            vehiclePlate: "MNO-321",
            vehicleModel: "Kia Picanto Ion 1.25",
            customerName: "Luisa Fernanda Duque",
            initialProgressState: VehicleProgressState.Recibido,
            createdAtUtc: nowUtc.AddHours(-15),
            initialNote: "Mantenimiento básico de 30.000 km."
        );
        order4.AdvanceProgress(VehicleProgressState.Diagnostico, nowUtc.AddHours(-12), "Todo en óptimo estado; no se detectaron daños adicionales.");
        order4.AdvanceProgress(VehicleProgressState.Reparacion, nowUtc.AddHours(-8), "Cambio de aceite y filtros regulares ejecutados.");
        order4.AdvanceProgress(VehicleProgressState.ControlDeCalidad, nowUtc.AddHours(-3), "Inspección de 25 puntos aprobada con éxito.");
        order4.AdvanceProgress(VehicleProgressState.ListoParaEntregar, nowUtc.AddHours(-1), "Vehículo lavado e higienizado listo para entrega.");
        WorkOrders[order4.Id] = order4;

        var link4 = new CustomerAccessLink(
            token: "demo-sin-adicionales",
            workshopId: workshop1.Id,
            workOrderId: order4.Id,
            createdAtUtc: nowUtc.AddHours(-15)
        );
        AccessLinks[link4.Token] = link4;

        // 6. Escenario 5: Taller Sur (Demostración de Aislamiento Multitenant)
        var order5 = new WorkOrder(
            id: "OT-201",
            workshopId: workshop2.Id,
            vehiclePlate: "SUR-555",
            vehicleModel: "Nissan Versa Advance",
            customerName: "Andrés Felipe Uribe",
            initialProgressState: VehicleProgressState.Recibido,
            createdAtUtc: nowUtc.AddHours(-10),
            initialNote: "Ingreso a Serviteca y Frenos del Sur para alineación."
        );
        WorkOrders[order5.Id] = order5;

        var link5 = new CustomerAccessLink(
            token: "demo-taller-sur",
            workshopId: workshop2.Id,
            workOrderId: order5.Id,
            createdAtUtc: nowUtc.AddHours(-10)
        );
        AccessLinks[link5.Token] = link5;
    }

    public void Reset(TimeProvider? timeProvider = null)
    {
        lock (_lock)
        {
            Workshops.Clear();
            WorkOrders.Clear();
            Quotes.Clear();
            AccessLinks.Clear();
            Seed(timeProvider ?? TimeProvider.System);
        }
    }

    public object GetLock() => _lock;
}
