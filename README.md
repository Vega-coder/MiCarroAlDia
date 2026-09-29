# Mi Carro al Día — Prototipo «Autofrenos del Norte»

> **Línea de ejecución directa:**
> ```bash
> dotnet run --project src/MiCarroAlDia.Web
> ```

---

## 1. Descripción del Proyecto
Solución web responsive y *mobile-first* desarrollada para el taller automotriz **Autofrenos del Norte**, optimizada para clientes que acceden desde celulares Android básicos sin necesidad de crear cuenta ni recordar contraseñas.

Permite al cliente:
1. **Consultar el avance de su vehículo** en una línea de tiempo clara con las 6 etapas fijas acordadas (*Recibido*, *Diagnóstico*, *Reparación*, *Control de calidad*, *Listo para entregar*, *Entregado*).
2. **Autorizar o rechazar trabajos adicionales ítem por ítem**, con cálculo transparente del IVA del 19% en pesos sin centavos.
3. **Confirmación obligatoria para rechazos de seguridad:** Si el cliente rechaza trabajos críticos (frenos, dirección, suspensión o llantas), el sistema exige confirmar expresamente el riesgo.
4. **Vigencia de 48 horas continuas:** Al expirar el plazo, el formulario se bloquea y se muestra el teléfono del taller para no detener puestos de trabajo.
5. **Registro atómico y definitivo:** Una vez enviada la respuesta, se emite un comprobante con fecha y hora, quedando bloqueada la modificación desde el celular.

---

## 2. Requisitos y Ejecución

* **Plataforma:** .NET 10 LTS SDK.
* **Sin dependencias externas:** No requiere base de datos externa ni Docker; utiliza repositorios en memoria concurrentes con datos sembrados listos para demostración.

### Ejecución de la aplicación web:
```bash
dotnet run --project src/MiCarroAlDia.Web
```
Una vez iniciado, abre tu navegador en la URL indicada por la consola (usualmente `http://localhost:5000` o `https://localhost:5001`).

### Ejecución de las pruebas unitarias y de integración:
```bash
dotnet test
```
*(36 pruebas automatizadas con 100% de éxito, validando cálculo de IVA, límites de 48h, atomicidad, prevención de duplicados/ítems ajenos, rechazos de seguridad, aislamiento por orden y pruebas HTTP).*

---

## 3. Escenarios de Demostración Sembrados

Al ingresar a la raíz (`/`), encontrarás un panel interactivo con tarjetas de acceso directo para cada uno de los 5 casos de prueba:

| Escenario | Token / Enlace | Vehículo | Descripción del Caso |
|---|---|---|---|
| **1. Cotización Pendiente (P0)** | `/Tracking/demo-activa` o `/t/demo-activa` | Renault Sandero (`ABC-123`) | **Flujo principal:** 28 horas restantes. Contiene pastillas de freno (Seguridad), mano de obra (Seguridad) y plumillas (General). Permite probar cálculos en vivo, advertencia de seguridad y envío definitivo. |
| **2. Cotización Respondida** | `/Tracking/demo-respondida` o `/t/demo-respondida` | Chevrolet Onix (`XYZ-789`) | Demuestra el **comprobante definitivo** con fecha/hora guardada y el bloqueo que impide cambiar la respuesta desde el celular. |
| **3. Cotización Vencida** | `/Tracking/demo-vencida` o `/t/demo-vencida` | Toyota Hilux (`KLR-456`) | Superó las 48 horas continuas (publicada hace 55h). Formulario cerrado y botón directo para llamar a Marcela. |
| **4. Sin Adicionales** | `/Tracking/demo-sin-adicionales` o `/t/demo-sin-adicionales` | Kia Picanto (`MNO-321`) | Estado vacío amigable (*«Por ahora no tienes trabajos adicionales pendientes»*) y vehículo listo para entrega. |
| **5. Aislamiento Multitenant** | `/Tracking/demo-taller-sur` o `/t/demo-taller-sur` | Nissan Versa (`SUR-555`) | Perteneciente a **Serviteca del Sur**. Demuestra que un enlace solo accede a los datos de su propio taller. |

---

## 4. Estructura del Código (Clean Architecture)

```
MiCarroAlDia/
├── src/
│   ├── MiCarroAlDia.Domain/                  # Núcleo de negocio puro (sin dependencias externas)
│   │   ├── Entities/                        # Workshop, WorkOrder, AdditionalQuote, QuoteItem, QuoteResponse
│   │   ├── Enums/                           # VehicleProgressState, ItemType, ItemCategory, CustomerDecision
│   │   ├── Exceptions/                      # DomainValidationException, DomainConflictException
│   │   └── Rules/                           # PricingCalculator (IVA 19%, redondeo al peso)
│   ├── MiCarroAlDia.Application/             # Casos de uso y orquestación
│   │   ├── Abstractions/                    # Contratos de repositorios (IWorkOrderRepository, etc.)
│   │   ├── CustomerTracking/                # Consulta de seguimiento y avance
│   │   ├── AdditionalResponses/             # Envío atómico de respuesta definitiva
│   │   └── Common/                          # Formato monetario COP y fecha/hora Colombia (UTC-5)
│   ├── MiCarroAlDia.Infrastructure/          # Adaptadores e infraestructura
│   │   ├── Persistence/InMemory/            # Base de datos en memoria concurrente y datos semilla
│   │   └── DependencyInjection.cs           # Registro de servicios
│   └── MiCarroAlDia.Web/                     # Capa de presentación (ASP.NET Core Razor Pages)
│       ├── Pages/                           # Index (portal demo) y Tracking/Index (pantalla cliente)
│       ├── wwwroot/                         # CSS mobile-first, JS reactivo y Bootstrap
│       └── Program.cs                       # Configuración y redirecciones cortas (/t/{token})
├── tests/
│   └── MiCarroAlDia.Tests/                  # Suite de pruebas xUnit (19 pruebas automatizadas)
├── docs/
│   ├── Alcance-y-supuestos.md               # Detalle de acuerdos, descartes y supuestos técnicos
│   └── Prompts-y-estrategia.md              # Registro completo de prompts y guía para sustentación
└── README.md
```

---

## 5. Documentos de la Entrega
* Consulta [Alcance-y-supuestos.md](file:///c:/Users/USUARIO/Desktop/SYSTUN/docs/Alcance-y-supuestos.md) para el desglose detallado de requisitos P0 vs P1 y justificaciones de negocio.
* Consulta [Prompts-y-estrategia.md](file:///c:/Users/USUARIO/Desktop/SYSTUN/docs/Prompts-y-estrategia.md) para el registro íntegro de interacción con la IA y la guía para la sustentación en vivo.
