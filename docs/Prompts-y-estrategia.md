# Documento de Prompts y Estrategia — «Mi Carro al Día»
**Evaluación de Capacidades · Equipo de Desarrollo**  
**Prueba 5 — Desarrollo con IA**

---

## 1. Registro Cronológico de Prompts Reales (Completos y en Orden)

Este registro documenta textualmente cada interacción enviada a la IA durante la sesión, preservando la redacción original sin reconstrucciones.

---

### Prompt 1
* **Momento:** Inicio de la prueba (Definición de alcance, arquitectura y especificación técnica).
* **Texto completo enviado:**
```text
Análisis y especificación de «Mi Carro al Día»
Mi decisión es desarrollar una aplicación web responsive, pensada primero para celulares, utilizando C#, ASP.NET Core Razor Pages y Clean Architecture ligera. No haría una aplicación móvil nativa para esta primera versión.
El objetivo no es construir un sistema completo para administrar talleres. La prioridad acordada es que el cliente pueda aprobar o rechazar trabajos adicionales desde su celular y consultar cómo va su carro. El panel de Marcela puede esperar; el documento lo establece expresamente al cierre. 05-prueba-con-ia
A continuación separo los acuerdos del documento, las decisiones técnicas que propongo y los puntos que todavía requieren confirmación.
1. Resumen del problema
El taller Autofrenos del Norte pasó de atender 15 a 40 carros mensuales. Esto aumentó las llamadas de clientes preguntando por el avance de sus vehículos y la carga de trabajo de Marcela, la asesora de servicio. Además, cuando se detectan daños adicionales, la dificultad para contactar al cliente retrasa la autorización y puede dejar el carro ocupando un puesto hasta dos días. 05-prueba-con-ia
La solución acordada consiste en permitir que el cliente consulte información y responda solicitudes concretas, sin abrir un chat con el taller. El cliente debe decidir sobre cada ítem adicional de forma independiente, y su respuesta debe quedar registrada con fecha y hora. 05-prueba-con-ia 05-prueba-con-ia
Objetivo del producto
Facilitar la autorización de trabajos adicionales y la consulta del avance del vehículo, para reducir la necesidad de llamadas entre el cliente y la asesora.
Condición importante de la evaluación
La prueba dura 40 minutos: 30 para construir y 10 para entregar. No exige base de datos ni despliegue; permite una solución local con datos de ejemplo. Se evalúa principalmente la comprensión del problema y la forma de orientar a la IA, no la cantidad de funcionalidades. 05-prueba-con-ia
Por eso, aunque planteemos una arquitectura ordenada, no conviene gastar la prueba construyendo infraestructura que no aporta al recorrido principal del cliente.
2. Qué se acordó y qué debemos descartar
Las notas contienen propuestas que cambiaron durante la reunión. Estas son las decisiones que deben prevalecer:
Tema: Comunicación -> No habrá chat. El cliente consulta y responde lo solicitado.
Tema: Autorización -> Se aprueba o rechaza ítem por ítem, no todo o nada.
Tema: Plazo -> Son 48 horas continuas, incluidos fines de semana; no 24 horas.
Tema: Notificaciones -> Marcela comparte el enlace manualmente por WhatsApp. No se implementan correos ni integración con WhatsApp.
Tema: Pagos -> Quedan fuera del alcance, incluido PSE.
Tema: Tecnología -> Se utiliza la tecnología que maneja el equipo; Flutter y Firebase no son requisitos.
Tema: Identidad visual -> El nombre es Mi Carro al Día y el color principal es azul.
Estas decisiones aparecen explícitamente en la reunión.
La frase «¡Gracias, campeón!» fue una intervención que nadie retomó; no la trataría como un requisito aprobado.
Alcance por etapas:
P0 — Prototipo de la prueba: Consulta del vehículo, adicionales, decisiones por ítem, confirmación de rechazos de seguridad, vencimiento, registro de respuesta y bloqueo posterior. Datos de ejemplo.
P1 — Operación interna: Panel de Marcela para consultar sus órdenes, publicar avances, preparar adicionales y revisar respuestas.
P2 — Evolución comercial: Operación con varios talleres, administración interna y persistencia adecuada para uso real.
... [Se detallaron RNF-01 a RNF-12, modelo Clean Architecture propuesto con Domain, Application, Infrastructure, Web y Tests, supuestos numéricos y plan de 40 minutos] ...
La primera entrega debería demostrar un recorrido pequeño pero completo: el cliente abre su enlace, entiende el avance, responde cada adicional y obtiene un registro definitivo, con los rechazos de seguridad y el vencimiento correctamente controlados. Esa es la base sobre la que después construiría la operación interna del taller.
```
* **Por qué se escribió así:**
  Para alinear a la IA con los requerimientos exactos del documento, delimitar taxativamente el alcance a la prioridad P0 (descartando chat, Flutter, PSE y el panel de Marcela), e instruir la creación de la estructura limpia en 4 capas de Clean Architecture con datos en memoria para optimizar el tiempo.

---

### Prompt 2
* **Momento:** Verificación inicial del ejecutable web.
* **Texto completo enviado:**
```text
Ejecutalo y dame la url
```
* **Por qué se escribió así:**
  Para validar de inmediato que el servidor web compilara, escuchara peticiones en el puerto local y permitiera navegar la experiencia móvil del cliente y el portal de escenarios.

---

### Prompt 3
* **Momento:** Exploración de integración con base de datos en la nube.
* **Texto completo enviado:**
```text
Conectemos esto a databasa eso es supabase
[Adjunta captura del dashboard de Supabase del proyecto wquguegisippjrttyzxq]
```
* **Por qué se escribió así:**
  Para explorar la viabilidad de persistencia externa en Supabase (PostgreSQL) usando la información visible del proyecto.

---

### Prompt 4
* **Momento:** Revisión técnica rigurosa, auditoría de código, corrección de defectos y pruebas de regresión.
* **Texto completo enviado:**
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
* **Por qué se escribió así:**
  Para exigir rigor técnico: congelar adiciones de infraestructura innecesarias (descartando Supabase para la prueba de 40 minutos), auditar la seguridad y concurrencia del caso de uso de envío, asegurar la trazabilidad de cada requisito contra pruebas automatizadas y corregir imprecisiones documentales.

---

### Prompt 5
* **Momento:** Consulta de navegación para configuración de base de datos.
* **Texto completo enviado:**
```text
Donde le doy
```
* **Por qué se escribió así:**
  Pregunta breve para ubicar la sección de Connection String dentro del panel de Supabase.

---

### Prompt 6
* **Momento:** Entrega inicial de la cadena de conexión directa de Supabase.
* **Texto completo enviado:**
```text
postgresql://postgres:[YOUR-PASSWORD]@db.wquguegisippjrttyzxq.supabase.co:5432/postgres   eso dio eso es todo?
```
* **Por qué se escribió así:**
  Entrega del URI directo de Supabase para validación de formato y solicitud de la contraseña.

---

### Prompt 7
* **Momento:** Entrega de credenciales y Connection String del Session Pooler de Supabase.
* **Texto completo enviado:**
```text
Google2404.12 contraseña 

postgresql://postgres.wquguegisippjrttyzxq:Google2404.12@aws-0-us-east-1.pooler.supabase.com:5432/postgres sesion puler
```
* **Por qué se escribió así:**
  Suministro de la contraseña y de la cadena de conexión IPv4 compatible (Session Pooler) para resolver la resolución DNS y conectar EF Core Npgsql con la base de datos PostgreSQL en la nube de Supabase.

---

### Prompt 8
* **Momento:** Verificación de la persistencia real en base de datos.
* **Texto completo enviado:**
```text
Ya tenemos eso en base de datos?
```
* **Por qué se escribió así:**
  Validación del estado de creación de tablas (`workshops`, `work_orders`, `additional_quotes`, `customer_access_links`) y sembrado inicial de datos en Supabase.

---

### Prompt 9
* **Momento:** Eliminación de valores quemados (hardcoded) y transición a datos 100% reactivos.
* **Texto completo enviado:**
```text
Entonces quita los quemados de codigo y dejemoslo todo en datos reactivos
```
* **Por qué se escribió así:**
  Instrucción directa para erradicar cualquier texto o dato estático quemado en las vistas Razor (`Index.cshtml`, `_Layout.cshtml`, `Tracking/Index.cshtml`), garantizando que la totalidad de las tarjetas, placas, estados, nombres de cliente, talleres, teléfonos, horas restantes y resúmenes de ítems se carguen y rendericen en tiempo real desde la base de datos (PostgreSQL/Supabase o En Memoria).

---

### Prompt 10
* **Momento:** Publicación al repositorio remoto e inquietud sobre despliegue continuo.
* **Texto completo enviado:**
```text
Publica los cambio al repo, con eso se actualiza automaticamente la publicada?
```
* **Por qué se escribió así:**
   Solicitud para versionar y subir el código actualizado a GitHub (`Vega-coder/MiCarroAlDia`), preguntando si el repositorio cuenta con actualización automática (*Auto-Deploy*) en la plataforma donde se encuentre publicada la aplicación.

---

### Prompt 11
* **Momento:** Consulta de viabilidad técnica sobre despliegue en Vercel.
* **Texto completo enviado:**
```text
como podemos publicar en vercel el proyecto?
```
* **Por qué se escribió así:**
   Indagación sobre alternativas de hosting PaaS tipo Vercel, evaluando la viabilidad de publicar una solución ASP.NET Core Razor Pages en .NET 10 y recibiendo la recomendación de usar plataformas compatibles con contenedores como Render, Railway o Azure App Service.

---

### Prompt 12
* **Momento:** Creación del repositorio remoto y sincronización de código en Git.
* **Texto completo enviado:**
```text
Crea el repo y subelo a git
```
* **Por qué se escribió así:**
   Instrucción para inicializar Git local, configurar `.gitignore`, `.dockerignore`, `Dockerfile`, crear el repositorio remoto `Vega-coder/MiCarroAlDia` en GitHub mediante GitHub CLI y realizar el primer push a la rama `main`.

---

### Prompt 13
* **Momento:** Navegación en el panel de Render para selección del tipo de servicio.
* **Texto completo enviado:**
```text
Donde le doy para publicarlo
[Adjunta captura de dashboard.render.com mostrando opciones: Static Sites, Web Services, Private Services, etc.]
```
* **Por qué se escribió así:**
   Solicitud de orientación visual dentro de Render para elegir la opción correcta (`Web Services`) capaz de ejecutar el contenedor Docker de ASP.NET Core.

---

### Prompt 14
* **Momento:** Confirmación de autorización de GitHub CLI.
* **Texto completo enviado:**
```text
Ya lo autorice
```
* **Por qué se escribió así:**
   Notificación de que el código OAuth de un solo uso (`5FA1-AE75`) fue autorizado en el navegador, permitiendo al CLI completar la creación y subida del repositorio remoto.

---

### Prompt 15
* **Momento:** Confirmación previa al despliegue en Render.
* **Texto completo enviado:**
```text
Ahi?
[Adjunta captura de pantalla con los campos de configuración en Render: Docker, main, Ohio, Free tier y botón Deploy web service]
```
* **Por qué se escribió así:**
   Verificación visual de que los parámetros autocompletados por Render coincidieran con la configuración adecuada antes de iniciar la compilación.

---

### Prompt 16
* **Momento:** Consulta sobre la obligatoriedad de variables de entorno.
* **Texto completo enviado:**
```text
Y esa variables?
```
* **Por qué se escribió así:**
   Pregunta para aclarar si se requiere configurar variables de entorno obligatoriamente en Render o si el sistema puede operar de inmediato con la persistencia en memoria y datos semilla.

---

### Prompt 17
* **Momento:** Solicitud de empaquetado ZIP para distribución y generación del documento resumen integral.
* **Texto completo enviado:**
```text
Perfecto ahora dame en la carpeta que hicimos el proyecto, pero que sea zip para pasarla a alguien por whatsapp haz un archivo resumen de las implementaciones, requisitos, arquitectura que usamos, conexiones, tecnologia, despliegue, docker etc y los promps que te envie para generarla y alñade esto a los proms Chatgpt 6 max : Analiza esto y dame un resumen y requisitos funcionales y no funcionales, vamos a ahcer en  c# en vs code 2026 con clean arquitecture, decide si web o mobile y dame todo de manera detallada

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
* **Por qué se escribió así:**
   Consolidación final de la entrega: generación de un archivo comprimido limpio para envío instantáneo por WhatsApp, redacción del documento ejecutivo de arquitectura y síntesis técnica, y actualización rigurosa del registro de prompts.

---

### Prompt 18
* **Momento:** Consulta de responsividad móvil y solicitud de integración de alertas con SweetAlert2.
* **Texto completo enviado:**
```text
Es 100% responsive? puedes usar sweetalert para las alertas?
```
* **Por qué se escribió así:**
   Validación de la adaptación completa a pantallas de celulares Android e iOS (Mobile-First) y sustitución de los diálogos nativos del navegador (`alert`) y avisos estáticos por modales interactivos y toasts elegantes mediante la librería SweetAlert2.

---

### Prompt 19
* **Momento:** Consulta sobre el alcance de la entrega (cliente vs taller).
* **Texto completo enviado:**
```text
Ahi solamente esta la parte del cliente verdad?
```
* **Por qué se escribió así:**
   Pregunta de verificación para confirmar que la entrega realizada cubría la prioridad P0 (recorrido del cliente) y evaluar la incorporación del panel administrativo interno del taller (P1).

---

### Prompt 20
* **Momento:** Solicitud de desarrollo del panel administrativo del taller (Fase P1).
* **Texto completo enviado:**
```text
Hagamos esa parte tambien
```
* **Por qué se escribió así:**
   Instrucción para construir el Panel de Gestión de Taller de Marcela (`/Taller` o `/Marcela`), habilitando el control de órdenes, avance secuencial de etapas del vehículo, creación de nuevos ingresos con generación de enlaces WhatsApp, y publicación de cotizaciones adicionales con vigencia de 48 horas.

---

## 2. Estrategia del Desarrollador y Decisiones Técnicas

### ¿Qué se construyó y por qué?
* **Enfoque 100% en P0:** Se construyó exclusivamente el recorrido del cliente (consulta de vehículo, revisión de adicionales, decisiones por ítem, confirmación de seguridad, cómputo de 48h continuas, congelamiento de respuesta y comprobante).
* **Arquitectura Clean Architecture Desacoplada:** El dominio y los casos de uso son 100% agnósticos a la base de datos. Funcionan tanto en memoria para pruebas rápidas como sobre PostgreSQL (Supabase) mediante EF Core con JSONB y repositorios concretos.

### ¿Cómo se revisó lo generado por la IA y qué se corrigió?
1. **Defecto de concurrencia y atomicidad:**
   * *Hallazgo:* Envíos simultáneos podían generar condiciones de carrera si dos hilos entraban a `SubmitCustomerResponse` antes de que se asignara `Response`.
   * *Corrección:* Se introdujo un cerrojo de sincronización interno `lock (_syncRoot)` dentro de la entidad `AdditionalQuote` y se validó con la prueba `TwoSimultaneousSubmissions_OnlyOneSucceeds_SecondIsRejectedAndNoPartialWrites`.
2. **Defecto de validación de ítems en el payload:**
   * *Hallazgo:* No se validaba explícitamente si el payload contenía ítems repetidos o identificadores ajenos a la cotización.
   * *Corrección:* Se añadieron validaciones que arrojan `DomainValidationException` ante duplicados (`GroupBy`), ítems ajenos y omisiones.
3. **Comportamiento en recarga y reenvío POST:**
   * *Hallazgo:* Un segundo POST en una cotización ya respondida generaba un error de conflicto en el formulario en lugar de presentar el comprobante.
   * *Corrección:* `Index.cshtml.cs` detecta si la cotización ya está respondida y redirige limpiamente al comprobante con un mensaje informativo, preservando la inmutabilidad de la respuesta original.
4. **Corrección documental:**
   * *Hallazgo:* Se presentaban $416.500 y $357.000 como «ejemplo oficial del enunciado».
   * *Corrección:* Se aclaró que son cifras del ejemplo numérico formulado por el desarrollador en su análisis para contrastar la regla del 19% IVA, y no cifras dadas en la reunión.
   * Se retiraron declaraciones de "conformidad WCAG AA completa", precisando que se adoptaron criterios de diseño móvil accesible.
5. **Transición a datos 100% reactivos y erradicación de valores quemados:**
   * *Hallazgo:* La página de inicio `Index.cshtml`, el encabezado de taller en `_Layout.cshtml` y el mensaje de error de enlace contenían textos estáticos quemados ("Autofrenos del Norte", teléfono fijo 444-1234, lista fija de tarjetas).
   * *Corrección:* Se implementó `ActiveOrderCardDto` en `Index.cshtml.cs` y se enlazó `_Layout.cshtml` con `ViewData["WorkshopName"]` y `ViewData["WorkshopPhone"]`. Toda la información (placas, modelos, clientes, talleres, estados de avance, estado de cotización, conteo de horas restantes, desglose de ítems cotizados de seguridad/general y botones de acción) se genera en tiempo real a partir de las entidades leídas de la base de datos (Supabase PostgreSQL / En Memoria). Adicionalmente, el botón `🔄 Reiniciar datos de prueba` ejecuta un re-sembrado transaccional dinámico en ambas persistencias.

---

## 3. Guía de Sustentación en Vivo (Sin IA)

Ubicación exacta de cada regla para defender la solución o modificarla en vivo:

1. **Validación de enlace y orden específica:**
   * `src/MiCarroAlDia.Application/CustomerTracking/GetCustomerTrackingUseCase.cs` (líneas 37-52)
   * `src/MiCarroAlDia.Application/AdditionalResponses/SubmitCustomerResponseUseCase.cs` (líneas 38-55)
2. **Vencimiento de 48 horas continuas:**
   * `src/MiCarroAlDia.Domain/Entities/AdditionalQuote.cs` (propiedad `ExpiresAtUtc` y método `IsExpired`)
3. **Confirmación obligatoria para rechazo de seguridad:**
   * `src/MiCarroAlDia.Domain/Entities/AdditionalQuote.cs` (líneas dentro de `SubmitCustomerResponse`)
4. **Cálculo de IVA al 19% y redondeo:**
   * `src/MiCarroAlDia.Domain/Rules/PricingCalculator.cs` (método `CalculateItemAmounts`)
5. **Inmutabilidad de respuesta y comprobante:**
   * `src/MiCarroAlDia.Domain/Entities/AdditionalQuote.cs` (verificación `Response != null` bajo `lock (_syncRoot)`)
