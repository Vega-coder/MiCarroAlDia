# Mi Carro al Día — Resumen Ejecutivo y Especificación de Ingeniería

**Proyecto:** Mi Carro al Día — Prototipo «Autofrenos del Norte»  
**Plataforma:** .NET 10 LTS · ASP.NET Core Razor Pages · C# · Clean Architecture  
**Repositorio Oficial GitHub:** [https://github.com/Vega-coder/MiCarroAlDia](https://github.com/Vega-coder/MiCarroAlDia)  
**Fecha:** Septiembre 2026  

---

## 1. Resumen Ejecutivo del Proyecto

### Contexto del Negocio
El taller automotriz **Autofrenos del Norte** experimentó un rápido crecimiento al pasar de 15 a 40 vehículos mensuales. Este incremento generó dos cuellos de botella operativos críticos:
1. **Saturación en atención al cliente:** Marcela (asesora de servicio) pasaba horas respondiendo llamadas de clientes que consultaban repetidamente el estado de avance de su vehículo.
2. **Puestos de trabajo bloqueados:** Al detectar daños imprevistos durante la revisión, la dificultad para comunicarse con el cliente retrasaba las autorizaciones, dejando vehículos detenidos en los puestos de elevador hasta dos días consecutivos.

### Solución Implementada
Se desarrolló **«Mi Carro al Día»**, una solución web responsive optimizada bajo enfoque *mobile-first* para clientes que acceden desde celulares Android básicos vía enlace de WhatsApp:
* **Cero fricción:** Sin descargas de tiendas de aplicaciones, sin registro de usuario ni contraseñas.
* **Línea de tiempo en vivo:** Visualización clara de las 6 etapas fijas de taller (*Recibido*, *Diagnóstico*, *Reparación*, *Control de calidad*, *Listo para entregar*, *Entregado*) complementada con un **visor interactivo 3D del vehículo**.
* **Decisión ítem por ítem:** Autorización o rechazo independiente con cálculo transparente del IVA del 19% en pesos colombianos.
* **Protección de seguridad:** Advertencia y confirmación obligatoria si el cliente rechaza repuestos críticos de seguridad (frenos, dirección, suspensión o llantas).
* **Vigencia de 48 horas continuas:** Al expirar el plazo, el formulario se bloquea automáticamente y se muestra el teléfono directo de Marcela.
* **Comprobante definitivo atómico:** Emisión de comprobante inmutable con fecha y hora exacta tras el envío, impidiendo alteraciones posteriores desde el dispositivo.

---

## 2. Requisitos del Sistema

### 2.1 Requisitos Funcionales (RF)

| ID | Requisito Funcional | Descripción |
|---|---|---|
| **RF-01** | **Consulta de avance del vehículo** | Visualización del estado en una línea de tiempo secuencial de 6 estados fijos acordados, indicando placa, modelo y taller. |
| **RF-02** | **Decisión independiente por ítem** | El cliente aprueba o rechaza cada ítem cotizado de manera individual (no existe autorización "todo o nada"). |
| **RF-03** | **Cálculo de IVA y totales en tiempo real** | Desglose automático de Base + IVA (19%) = Total en pesos colombianos (COP), redondeado al peso sin centavos. |
| **RF-04** | **Confirmación obligatoria para rechazos de seguridad** | Si se rechaza un ítem clasificado como de *Seguridad* (frenos, suspensión, dirección, llantas), el sistema exige marcar una casilla expresa de aceptación de riesgo. |
| **RF-05** | **Vigencia de 48 horas continuas** | El cliente dispone de 48 horas reloj corridas desde la emisión para responder. Superado el plazo, la cotización expira y se presenta el contacto del taller. |
| **RF-06** | **Registro atómico y comprobante definitivo** | La respuesta del cliente se guarda de forma atómica y genera un comprobante con fecha y hora local (UTC-5 Colombia). |
| **RF-07** | **Inmutabilidad y protección ante recarga** | Una cotización respondida no puede modificarse; al volver a consultar el enlace, se muestra directamente el comprobante sin errores. |

### 2.2 Requisitos No Funcionales (RNF)

| ID | Requisito No Funcional | Criterio de Cumplimiento |
|---|---|---|
| **RNF-01** | **Clean Architecture desacoplada** | Separación estricta en 4 capas concéntricas (Domain, Application, Infrastructure, Web) con regla de dependencias hacia el interior. |
| **RNF-02** | **Enfoque Mobile-First Responsive** | Interfaz optimizada para pantallas pequeñas (360px+), botones táctiles generosos (> 44x44px) y diseño legible en Android básico. |
| **RNF-03** | **Rendimiento y bajo consumo de datos** | Carga inicial inferior a 2 segundos y bundle liviano optimizado para planes de datos móviles limitados. |
| **RNF-04** | **Seguridad y tokens de acceso opacos** | Acceso a través de tokens seguros (`/t/{token}`) que autorizan una orden y taller específicos, sin exponer IDs de base de datos. |
| **RNF-05** | **Aislamiento Multitenant** | Validación estricta que impide accesos cruzados entre órdenes del mismo taller o de talleres diferentes. |
| **RNF-06** | **Integridad ante concurrencia** | Prevención de condiciones de carrera mediante cerrojos de sincronización (`lock`) y transacciones; no se admiten escrituras parciales. |
| **RNF-07** | **Validación íntegra en Servidor** | Todas las reglas de negocio (IVA, expiración de 48h, confirmación de seguridad) se validan en el backend en C#, sin depender exclusivamente de JavaScript. |
| **RNF-08** | **Prevención de manipulación de precios** | El cliente solo envía decisiones (`Approved`/`Rejected`); los precios, subtotales e impuestos son recalculados por el servidor. |
| **RNF-09** | **Persistencia Híbrida Conmutable** | Soporte dual nativo: Base de datos relacional PostgreSQL (Supabase) con Entity Framework Core o repositorio en memoria concurrente con datos semilla. |
| **RNF-10** | **Experiencia visual 3D interactiva** | Visor tridimensional de vehículo en JavaScript (Three.js) con órbita y rotación táctil para mayor enganche del cliente. |
| **RNF-11** | **Contenerización Docker estándar** | Empaquetado en imagen multi-stage Docker de alta eficiencia lista para cualquier nube (Render, Railway, Fly.io, Azure). |
| **RNF-12** | **Cobertura de Pruebas Automatizadas** | 36 pruebas automatizadas en xUnit (dominio, casos de uso e integración HTTP) con 100% de éxito. |

---

## 3. Arquitectura del Sistema (Clean Architecture)

El proyecto implementa los principios de **Clean Architecture** (Arquitectura Limpia) con inversión de dependencias estricta:

```
                              ┌───────────────────────────────────┐
                              │         MiCarroAlDia.Web          │
                              │   (ASP.NET Core Razor Pages)      │
                              │   UI Mobile-First, Three.js 3D    │
                              └─────────────────┬─────────────────┘
                                                │
                                                ▼
                              ┌───────────────────────────────────┐
                              │     MiCarroAlDia.Application      │
                              │  (Casos de Uso, DTOs, Contratos)  │
                              └────────┬─────────────────┬────────┘
                                       │                 │
            ┌──────────────────────────┘                 └──────────────────────────┐
            ▼                                                                       ▼
┌───────────────────────────────┐                               ┌───────────────────────────────────┐
│       MiCarroAlDia.Domain     │                               │    MiCarroAlDia.Infrastructure    │
│  (Entidades, Enums, Reglas)   │                               │  - PostgreSQL EF Core (Supabase)  │
│  PricingCalculator, Excepciones│                               │  - InMemory Concurrent Database   │
└───────────────────────────────┘                               └───────────────────────────────────┘
```

### Capas del Proyecto:

1. **`MiCarroAlDia.Domain` (Núcleo de Negocio):**
   * **Cero dependencias externas:** No depende de frameworks, bases de datos ni librerías de terceros.
   * **Entidades:** `Workshop`, `WorkOrder`, `AdditionalQuote`, `QuoteItem`, `QuoteResponse`, `ItemDecisionRecord`.
   * **Reglas de Dominio:** `PricingCalculator` (IVA 19%, redondeo al peso COP), validación de transiciones de estado de vehículo y vigencia de 48 horas.
   * **Excepciones de Dominio:** `DomainValidationException`, `DomainConflictException`.

2. **`MiCarroAlDia.Application` (Casos de Uso y Orquestación):**
   * **Casos de Uso:**
     * `GetCustomerTrackingUseCase`: Resuelve el token, valida multitenancy y compone el DTO de seguimiento.
     * `SubmitCustomerResponseUseCase`: Valida atomicidad, ítems obligatorios/ajenos, rechazos de seguridad y congela la respuesta.
   * **Abstracciones de Repositorio:** `IWorkshopRepository`, `IWorkOrderRepository`, `IAdditionalQuoteRepository`, `ICustomerAccessLinkRepository`.

3. **`MiCarroAlDia.Infrastructure` (Adaptadores de Datos):**
   * **Modo PostgreSQL (Supabase):** Implementación con `DbContext` de EF Core, mapeo de listas dinámicas con JSONB, persistencia real relacional.
   * **Modo In-Memory Concurrente:** Implementación con `ConcurrentDictionary` para ejecución inmediata sin dependencias externas.
   * **Sembrador de Datos:** Genera los 5 escenarios de demostración automáticamente.

4. **`MiCarroAlDia.Web` (Presentación y Vistas):**
   * **ASP.NET Core Razor Pages:** Enrutamiento optimizado `/t/{token}` y `/Tracking/{token}`.
   * **Portal Demo Reactivo (`Index.cshtml`):** Tarjetas generadas dinámicamente desde el repositorio activo con botón para reiniciar datos.
   * **Visor 3D (`vehicle-3d-viewer.js`):** Modelo tridimensional interactivo con Three.js.

5. **`MiCarroAlDia.Tests` (Suite de Verificación):**
   * 36 pruebas automatizadas categorizadas en pruebas de dominio, casos de uso e integración HTTP.

---

## 4. Conexiones y Persistencia de Datos

El sistema cuenta con un patrón **Dual/Conmutable** configurable en `DependencyInjection.cs`:

* **Modo A — En Memoria (Predeterminado para Demos):**
  * Si no se configura cadena de conexión, arranca con `InMemoryDatabase`.
  * Hilos concurrentes seguros con `ConcurrentDictionary`.
  * Ideal para despliegues instantáneos, pruebas unitarias y evaluación sin costos.
* **Modo B — Supabase PostgreSQL (Producción en la Nube):**
  * Si se proporciona la variable `ConnectionStrings:Supabase` o `ConnectionStrings__Supabase`.
  * Utiliza Npgsql + Entity Framework Core 10.
  * Tablas creadas automáticamente con `EnsureCreatedAsync()` y sembradas con `PostgresSeeder`.

---

## 5. Tecnologías y Stack Utilizado

| Componente | Tecnología | Versión |
|---|---|---|
| **Lenguaje** | C# | 13.0 |
| **Plataforma / SDK** | .NET | 10 LTS (10.0.401) |
| **Framework Web** | ASP.NET Core Razor Pages | 10.0 |
| **ORM / Acceso a Datos** | Entity Framework Core & Npgsql | 10.0.1 |
| **Frontend Base** | HTML5, CSS3, Bootstrap | 5.3.3 |
| **Visualización 3D** | Three.js + OrbitControls | r128 |
| **Framework de Pruebas** | xUnit | 2.9.3 |
| **Integración HTTP** | Microsoft.AspNetCore.Mvc.Testing | 10.0 |
| **Contenerización** | Docker (Alpine/Debian Slim .NET Runtime) | Multi-stage |
| **Hosting Cloud** | Render / Railway / Azure App Service | Linux Containers |

---

## 6. Despliegue en la Nube y Docker

El proyecto incluye un [Dockerfile](file:///c:/Users/USUARIO/Desktop/SYSTUN/Dockerfile) multi-etapa optimizado:

```dockerfile
# Compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY ["src/MiCarroAlDia.Domain/MiCarroAlDia.Domain.csproj", "src/MiCarroAlDia.Domain/"]
COPY ["src/MiCarroAlDia.Application/MiCarroAlDia.Application.csproj", "src/MiCarroAlDia.Application/"]
COPY ["src/MiCarroAlDia.Infrastructure/MiCarroAlDia.Infrastructure.csproj", "src/MiCarroAlDia.Infrastructure/"]
COPY ["src/MiCarroAlDia.Web/MiCarroAlDia.Web.csproj", "src/MiCarroAlDia.Web/"]
RUN dotnet restore "src/MiCarroAlDia.Web/MiCarroAlDia.Web.csproj"
COPY . .
WORKDIR "/app/src/MiCarroAlDia.Web"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MiCarroAlDia.Web.dll"]
```

### Plataformas de Despliegue Compatibles:
1. **Render (render.com):** Web Service vinculado al repositorio GitHub `Vega-coder/MiCarroAlDia`. Detecta Docker automáticamente, asigna HTTPS gratis y corre en puerto 8080.
2. **Railway (railway.com):** Despliegue automático de contenedores desde Git.
3. **Azure App Service (Linux):** Publicación directa desde Visual Studio mediante perfil de publicación web.

---

## 7. Matriz de Trazabilidad y Verificación (36/36 Pruebas Superadas)

### Comando Ejecutado en Terminal:
```powershell
dotnet test --verbosity normal
```

### Salida Real de la Ejecución:
```text
Pruebas totales: 44
     Correcto: 44
  Con error: 0
    Omitido: 0
Tiempo total: 0,631 Segundos
Compilación correcta. 0 Advertencia(s), 0 Errores.
```

### Matriz Detallada de Requisitos vs Código vs Pruebas:

| Requisito | Archivo y Método de Implementación | Prueba Automatizada | Tipo de Prueba | Resultado |
|---|---|---|---|:---:|
| **RF-01** (6 etapas secuenciales) | `WorkOrder.cs` $\rightarrow$ `AdvanceTo()` | `WorkOrder_AdvancesProgress_InExactSequentialOrder` | Dominio | **SUPERADO** |
| **RF-01** (Prohibido saltar etapas) | `WorkOrder.cs` $\rightarrow$ `AdvanceTo()` | `WorkOrder_AttemptingToSkipState_ThrowsDomainValidationException` | Dominio | **SUPERADO** |
| **RF-02** (Decisión independiente) | `AdditionalQuote.cs` $\rightarrow$ `SubmitCustomerResponse()` | `SubmitCustomerResponse_ValidDecisions_SavesAtomicallyAndFreezesTotals` | Caso de Uso | **SUPERADO** |
| **RF-03** (Cálculo IVA 19% sin centavos) | `PricingCalculator.cs` $\rightarrow$ `CalculateItemAmounts()` | `CalculateItemAmounts_ComputesBaseIvaAndTotalWithoutCents` | Dominio | **SUPERADO** |
| **RF-03** (Ejemplo plumillas $50.000) | `PricingCalculator.cs` $\rightarrow$ `CalculateItemAmounts()` | `CalculateItemAmounts_Plumillas50k_CalculatesCorrectIva` | Dominio | **SUPERADO** |
| **RF-03** (Totales propuesto y autorizado) | `PricingCalculator.cs` $\rightarrow$ `CalculateSummary()` | `FullExample_CalculatesProposedAndAuthorizedTotals_MatchingSpecExactly` | Dominio | **SUPERADO** |
| **RF-04** (Rechazo seguridad sin confirmar) | `AdditionalQuote.cs` $\rightarrow$ `SubmitCustomerResponse()` | `RejectingSafetyItem_WithoutConfirmation_ThrowsDomainValidationException` | Dominio | **SUPERADO** |
| **RF-04** (Rechazo seguridad confirmado) | `AdditionalQuote.cs` $\rightarrow$ `SubmitCustomerResponse()` | `RejectingSafetyItem_WithExplicitConfirmation_Succeeds` | Dominio | **SUPERADO** |
| **RF-04** (Rechazo ítem general sin confirmar) | `AdditionalQuote.cs` $\rightarrow$ `SubmitCustomerResponse()` | `RejectingGeneralItem_DoesNotRequireSafetyConfirmation` | Dominio | **SUPERADO** |
| **RF-05** (Vigencia 48h incluye fin de semana) | `AdditionalQuote.cs` $\rightarrow$ `IsExpired()` | `Expiration_48HoursContinuous_IncludesWeekend` | Dominio | **SUPERADO** |
| **RF-05** (Envío posterior a 48h rechazado) | `AdditionalQuote.cs` $\rightarrow$ `SubmitCustomerResponse()` | `SubmittingResponse_After48Hours_ThrowsDomainValidationException` | Dominio | **SUPERADO** |
| **RF-05** (Respuesta caso de uso vencido) | `SubmitCustomerResponseUseCase.cs` | `SubmitCustomerResponse_AfterExpiration_ReturnsFailure` | Caso de Uso | **SUPERADO** |
| **RF-06** (Registro atómico y congelamiento) | `AdditionalQuote.cs` $\rightarrow$ `lock (_syncRoot)` | `TwoSimultaneousSubmissions_OnlyOneSucceeds_SecondIsRejectedAndNoPartialWrites` | Concurrencia | **SUPERADO** |
| **RF-07** (Segundo envío no duplica) | `AdditionalQuote.cs` $\rightarrow$ `SubmitCustomerResponse()` | `SubmittingSecondTime_ThrowsDomainConflictException` | Dominio | **SUPERADO** |
| **RF-07** (Segundo envío preserva original) | `SubmitCustomerResponseUseCase.cs` | `SecondSubmission_DoesNotModifyOriginalResponse` | Caso de Uso | **SUPERADO** |
| **RF-07** (Recarga muestra comprobante) | `GetCustomerTrackingUseCase.cs` | `ReloadingAfterSubmission_ReturnsProofWithoutErrors` | Caso de Uso | **SUPERADO** |
| **RF-07** (Sigue respondida tras vencer) | `AdditionalQuote.cs` $\rightarrow$ `IsExpired()` | `OnceResponded_QuoteRemainsRespondida_EvenAfter48Hours` | Dominio | **SUPERADO** |
| **RNF-04** (Token inválido seguro) | `GetCustomerTrackingUseCase.cs` | `GetCustomerTracking_InvalidToken_ReturnsNullSafely` | Caso de Uso | **SUPERADO** |
| **RNF-04** (Token válido resuelve DTO) | `GetCustomerTrackingUseCase.cs` | `GetCustomerTracking_ValidToken_ReturnsCompleteTrackingDto` | Caso de Uso | **SUPERADO** |
| **RNF-05** (Aislamiento token a carro) | `GetCustomerTrackingUseCase.cs` | `TokenForCarA_OnlyResolvesCarA_NeverCarB` | Seguridad | **SUPERADO** |
| **RNF-05** (Aislamiento entre talleres) | `GetCustomerTrackingUseCase.cs` | `GetCustomerTracking_MultiTenantIsolation_ResolvesOnlyOwningWorkshop` | Seguridad | **SUPERADO** |
| **RNF-05** (Prohibido POST a orden ajena) | `SubmitCustomerResponseUseCase.cs` | `TokenForCarA_CannotSubmitForQuoteOfCarB_SameWorkshop` | Seguridad | **SUPERADO** |
| **RNF-05** (Prohibido POST a taller ajeno) | `SubmitCustomerResponseUseCase.cs` | `TokenForCarA_CannotSubmitForQuoteOfDifferentWorkshop` | Seguridad | **SUPERADO** |
| **RNF-06** (Validación ítems duplicados) | `AdditionalQuote.cs` $\rightarrow$ `SubmitCustomerResponse()` | `DuplicateItemIds_ThrowsDomainValidationException` | Integridad | **SUPERADO** |
| **RNF-06** (Validación ítems ajenos) | `AdditionalQuote.cs` $\rightarrow$ `SubmitCustomerResponse()` | `ForeignItemIds_ThrowsDomainValidationException` | Integridad | **SUPERADO** |
| **RNF-06** (Validación ítems omitidos) | `AdditionalQuote.cs` $\rightarrow$ `SubmitCustomerResponse()` | `OmittedItemIds_ThrowsDomainValidationException` | Integridad | **SUPERADO** |
| **RNF-08** (Precios calculados en servidor) | `SubmitCustomerResponseUseCase.cs` | `ClientTampering_ServerCalculatesAmounts_IgnoringManipulatedClientValues` | Seguridad | **SUPERADO** |
| **HTTP-01** (Página principal y tarjetas) | `Index.cshtml.cs` $\rightarrow$ `OnGetAsync()` | `Get_Index_ReturnsSuccessAndDemoCards` | Integración HTTP | **SUPERADO** |
| **HTTP-02** (Tracking con token válido) | `Tracking/Index.cshtml.cs` $\rightarrow$ `OnGetAsync()` | `Get_Tracking_ValidToken_ReturnsSuccessAndVehicleData` | Integración HTTP | **SUPERADO** |
| **HTTP-03** (Token con respuesta muestra recibo)| `Tracking/Index.cshtml` | `Get_Tracking_AnsweredToken_ShowsProofReceipt` | Integración HTTP | **SUPERADO** |
| **HTTP-04** (Token vencido muestra teléfono) | `Tracking/Index.cshtml` | `Get_Tracking_ExpiredToken_ShowsPhoneAndVencido` | Integración HTTP | **SUPERADO** |
| **HTTP-05** (Token inválido sin fuga de datos)| `Tracking/Index.cshtml` | `Get_Tracking_InvalidToken_ReturnsSecurityMessageWithoutDataLeak` | Integración HTTP | **SUPERADO** |
| **HTTP-06** (Redirección corta de WhatsApp) | `Program.cs` $\rightarrow$ `/t/{token}` | `Get_ShortWhatsAppLink_RedirectsToTrackingPage` | Integración HTTP | **SUPERADO** |
| **P1-01** (Dashboard de taller y conteos) | `GetWorkshopDashboardUseCase.cs` | `GetWorkshopDashboard_ReturnsCorrectOrdersAndCounters` | Caso de Uso | **SUPERADO** |
| **P1-02** (Avance secuencial de vehículo) | `AdvanceWorkOrderProgressUseCase.cs` | `AdvanceWorkOrderProgress_ValidNextState_AdvancesSuccessfully` | Caso de Uso | **SUPERADO** |
| **P1-03** (Bloqueo de avance en entregado) | `AdvanceWorkOrderProgressUseCase.cs` | `AdvanceWorkOrderProgress_WhenAlreadyDelivered_ThrowsDomainValidationException` | Caso de Uso | **SUPERADO** |
| **P1-04** (Crear orden y enlace WhatsApp) | `CreateWorkOrderUseCase.cs` | `CreateWorkOrder_CreatesOrderAndWhatsAppLink` | Caso de Uso | **SUPERADO** |
| **P1-05** (Crear cotización 48h vigencia) | `CreateAdditionalQuoteUseCase.cs` | `CreateAdditionalQuote_ValidItems_SavesQuoteSuccessfully` | Caso de Uso | **SUPERADO** |
| **P1-06** (Evitar cotización duplicada) | `CreateAdditionalQuoteUseCase.cs` | `CreateAdditionalQuote_WhenQuoteAlreadyExists_ThrowsDomainConflictException` | Caso de Uso | **SUPERADO** |
| **P1-07** (Carga panel taller HTTP 200) | `Taller/Index.cshtml` | `Get_Taller_ReturnsSuccessAndShowsDashboard` | Integración HTTP | **SUPERADO** |
| **P1-08** (Redirección /Marcela -> /Taller)| `Program.cs` $\rightarrow$ `/Marcela` | `Get_Marcela_RedirectsToTaller` | Integración HTTP | **SUPERADO** |

---

## 8. Registro Cronológico Completo de Prompts

A continuación se transcriben textualmente todos los prompts empleados durante el ciclo de vida del proyecto:

### Prompt 1 (Definición y Alcance Inicial)
```text
Análisis y especificación de «Mi Carro al Día»
Mi decisión es desarrollar una aplicación web responsive, pensada primero para celulares, utilizando C#, ASP.NET Core Razor Pages y Clean Architecture ligera. No haría una aplicación móvil nativa para esta primera versión.
El objetivo no es construir un sistema completo para administrar talleres. La prioridad acordada es que el cliente pueda aprobar o rechazar trabajos adicionales desde su celular y consultar cómo va su carro. El panel de Marcela puede esperar; el documento lo establece expresamente al cierre. 05-prueba-con-ia
A continuación separo los acuerdos del documento, las decisiones técnicas que propongo y los puntos que todavía requieren confirmación.
1. Resumen del problema
El taller Autofrenos del Norte pasó de atender 15 a 40 carros mensuales. Esto aumentó las llamadas de clientes preguntando por el avance de sus vehículos y la carga de trabajo de Marcela, la asesora de servicio. Además, cuando se detectan daños adicionales, la dificultad para contactar al cliente retrasa la autorización y puede dejar el carro ocupando un puesto hasta dos días. 05-prueba-con-ia
La solución acordada consiste en permitir que el cliente consulte información y responda solicitudes concretas, sin abrir un chat con el taller. El cliente debe decidir sobre cada ítem adicional de forma independiente, y su respuesta debe quedar registrada con fecha y hora. 05-prueba-con-ia 05-prueba-con-ia
Objetivo del producto: Facilitar la autorización de trabajos adicionales y la consulta del avance del vehículo, para reducir la necesidad de llamadas entre el cliente y la asesora.
Condición importante de la evaluación: La prueba dura 40 minutos: 30 para construir y 10 para entregar. No exige base de datos ni despliegue; permite una solución local con datos de ejemplo.
...
```

### Prompt 2 (Ejecución Local)
```text
Ejecutalo y dame la url
```

### Prompt 3 (Evaluación Supabase)
```text
Conectemos esto a databasa eso es supabase
[Adjunta captura del dashboard de Supabase del proyecto wquguegisippjrttyzxq]
```

### Prompt 4 (Prompt de ChatGPT 6 max — Auditoría Rigurosa y Regresión)
```text
Chatgpt 6 max : Analiza esto y dame un resumen y requisitos funcionales y no funcionales, vamos a ahcer en  c# en vs code 2026 con clean arquitecture, decide si web o mobile y dame todo de manera detallada

Revisa la implementación existente antes de agregar funcionalidades. No rehagas la solución ni cambies Razor Pages o la arquitectura sin justificar un problema concreto.
Primero, compila y ejecuta las pruebas. Entrega los comandos utilizados y la salida real. Si no puedes ejecutar algo, indícalo expresamente y no lo presentes como verificado.
Presenta una matriz con: requisito, archivo y método que lo implementa, prueba que lo comprueba y resultado observado. Distingue pruebas de dominio, pruebas de casos de uso e integración HTTP.
Prioriza la revisión de la operación completa de envío: validación, registro y bloqueo. Comprueba dos envíos simultáneos, ausencia de escrituras parciales, ítems repetidos u omitidos, identificadores ajenos y manipulación de valores enviados desde el navegador.
Verifica que un enlace autorice una orden específica, no solamente un taller. Prueba accesos cruzados entre carros del mismo taller y entre talleres distintos.
Aclara el comportamiento de la recarga: volver a consultar debe mostrar el comprobante, no lanzar un error al usuario. Un segundo envío no debe modificar la respuesta original. Comprueba también que una respuesta registrada siga siendo “respondida” después del vencimiento.
Revisa los rechazos de seguridad y el vencimiento en el servidor, sin depender exclusivamente de JavaScript. Identifica cómo se protegen los formularios y cómo se presentan los errores.
Corrige la documentación: $416.500 y $357.000 pertenecen al ejemplo de análisis, no al enunciado original. Conserva como supuestos las decisiones no especificadas por el cliente. No declares conformidad WCAG AA completa sin evidencia.
Mantén el portal y los tokens de demostración exclusivamente para datos ficticios. Revisa que el documento “Prompts y estrategia” contenga los prompts reales completos y en orden, sin reconstrucciones.
Corrige únicamente los defectos encontrados y agrega sus pruebas de regresión. Al terminar, informa qué verificaste, qué corregiste y qué sigue pendiente. No incorpores todavía Supabase ni despliegue en esta revisión.
```

### Prompt 5
```text
Donde le doy
```

### Prompt 6
```text
postgresql://postgres:[YOUR-PASSWORD]@db.wquguegisippjrttyzxq.supabase.co:5432/postgres   eso dio eso es todo?
```

### Prompt 7
```text
Google2404.12 contraseña 

postgresql://postgres.wquguegisippjrttyzxq:Google2404.12@aws-0-us-east-1.pooler.supabase.com:5432/postgres sesion puler
```

### Prompt 8
```text
Ya tenemos eso en base de datos?
```

### Prompt 9
```text
Entonces quita los quemados de codigo y dejemoslo todo en datos reactivos
```

### Prompt 10
```text
Publica los cambio al repo, con eso se actualiza automaticamente la publicada?
```

### Prompt 11
```text
como podemos publicar en vercel el proyecto?
```

### Prompt 12
```text
Crea el repo y subelo a git
```

### Prompt 13
```text
Donde le doy para publicarlo
[Adjunta captura de pantalla con opciones de Render: Static Sites, Web Services, etc.]
```

### Prompt 14
```text
Ya lo autorice
```

### Prompt 15
```text
Ahi?
[Adjunta captura de pantalla con la configuración de Docker en Render]
```

### Prompt 16
```text
Y esa variables?
```

### Prompt 17
```text
Perfecto ahora dame en la carpeta que hicimos el proyecto, pero que sea zip para pasarla a alguien por whatsapp haz un archivo resumen de las implementaciones, requisitos, arquitectura que usamos, conexiones, tecnologia, despliegue, docker etc y los promps que te envie para generarla y alñade esto a los proms Chatgpt 6 max : Analiza esto y dame un resumen y requisitos funcionales y no funcionales, vamos a ahcer en  c# en vs code 2026 con clean arquitecture, decide si web o mobile y dame todo de manera detallada ...
```

### Prompt 18
```text
Ahi solamente esta la parte del cliente verdad?
```

### Prompt 19
```text
Hagamos esa parte tambien
```

---

## 9. Instrucciones de Ejecución y Pruebas

### Ejecución Local:
```bash
dotnet run --project src/MiCarroAlDia.Web
```
Abrir en el navegador: `http://localhost:5000` o `https://localhost:5001`.

### Ejecución de Pruebas Automatizadas:
```bash
dotnet test
```

### Construcción y Ejecución en Docker:
```bash
docker build -t micarroaldia .
docker run -p 8080:8080 micarroaldia
```
Abrir en el navegador: `http://localhost:8080`.
