# Alcance y Supuestos — «Mi Carro al Día»
**Taller Autofrenos del Norte**  
**Prueba 5 — Desarrollo con IA**

---

## 1. Resumen Ejecutivo del Problema
Autofrenos del Norte pasó de 15 a 40 vehículos mensuales. Esto generó dos problemas principales:
1. **Llamadas constantes:** Los clientes llaman a toda hora preguntando cómo va el carro, sobrecargando a Marcela (asesora de servicio).
2. **Puestos bloqueados por demoras en autorización:** Al detectar un daño adicional durante el diagnóstico, Marcela llama al cliente, este no contesta y el vehículo se queda ocupando un puesto hasta por dos días.

---

## 2. Acuerdos de la Reunión vs. Ideas Descartadas

| Tema | Propuesta Inicial / Desvío | Decisión Acordada Final | Justificación |
|---|---|---|---|
| **Canal de comunicación** | Don Álvaro propuso chat tipo WhatsApp para hablar directo con el asesor. | **Sin chat.** El cliente solo consulta y responde lo que se le pide. | Marcela no puede contestar 40 chats. La gente solo quiere ver cómo va el carro y responder solicitudes. |
| **Aprobación de adicionales** | Don Álvaro propuso «todo o nada» (aprobar todo o nada). | **Decisión ítem por ítem.** | Marcela aclaró: el cliente aprueba los frenos pero no el cambio de plumillas. |
| **Plazo de respuesta** | Don Álvaro propuso 24 horas. | **48 horas continuas (incluye fines de semana).** | Marcela aclaró que muchos clientes revisan al día siguiente por la noche. Si se vence, el cliente ya no puede responder y ve el teléfono del taller. |
| **Ítems de seguridad** | Jairo pidió control estricto. | **Aviso claro y confirmación explícita obligatoria.** | Si rechaza frenos, dirección, suspensión o llantas, debe confirmar que de verdad lo rechaza («para que después no nos echen la culpa»). |
| **Notificaciones** | Don Álvaro propuso correos en cada cambio. | **Compartir enlace manual por WhatsApp.** | Los clientes casi no leen correo; Marcela envía el enlace manualmente por WhatsApp sin integrar APIs de terceros. |
| **Pagos** | Don Álvaro propuso pago con PSE. | **Descartado / Fuera de alcance.** | Jairo enfatizó: «Eso es otro proyecto; primero esto». |
| **Tecnología** | Sugerencia externa de Flutter con Firebase. | **Tecnología que domina el equipo (C#, ASP.NET Core Razor Pages, Clean Architecture).** | Permite entregar una solución web directa para Android sencillo sin fricción de instalación. |
| **Identidad visual** | Don Álvaro propuso «Pits», colores rojo/negro y la frase «¡Gracias, campeón!». | **Nombre «Mi Carro al Día», color predominante azul, tono profesional y claro.** | El rojo asusta y la frase informal fue descartada. Se eliminan tecnicismos («OT», «estado 3»). |
| **Aislamiento Multitaller** | Don Álvaro planteó ofrecerlo a otros talleres a futuro. | **Aislamiento estricto de datos modelado desde el inicio.** | Ningún taller debe ver la información de otro taller. |

---

## 3. Alcance por Fases

### Prioridad P0 — Prototipo Implementado (Entrega Actual)
* Acceso seguro por enlace con token único (sin login ni contraseñas), restringido a la orden y taller asociados.
* Consulta del vehículo y su avance en la secuencia estricta de 6 etapas:
  1. Recibido
  2. Diagnóstico
  3. Reparación
  4. Control de calidad
  5. Listo para entregar
  6. Entregado
* Consulta de trabajos adicionales con descripción, tipo (repuesto / mano de obra), cantidad, valor unitario, IVA del 19% y total.
* Aprobación o rechazo independiente ítem por ítem.
* Confirmación explícita obligatoria en el servidor para rechazos de ítems de seguridad (frenos, dirección, suspensión, llantas).
* Cómputo de vigencia de 48 horas continuas en el servidor; bloqueo de formulario al vencer y visualización del teléfono del taller.
* Envío atómico, prevención de condiciones de carrera y bloqueo definitivo de la respuesta (no modificable desde el celular).
* Generación de comprobante de respuesta con fecha, hora y totales autorizados; la recarga muestra el comprobante sin lanzar errores.
* Manejo de estados vacíos («Por ahora no tienes trabajos adicionales pendientes de revisar»).
* Repositorio en memoria concurrente con datos semilla exclusivamente ficticios para demostración y suite de pruebas automatizadas.

### Prioridad P1 — Operación Interna (Fase Posterior)
* Panel web interno para Marcela: consultar órdenes asignadas, registrar avances cortos, redactar adicionales y revisar respuestas de clientes.

### Prioridad P2 — Evolución Comercial
* Operación multi-taller con base de datos relacional duradera, autenticación de personal y administración global.

---

## 4. Supuestos Técnicos y de Negocio Documentados

Los siguientes puntos corresponden a decisiones de diseño adoptadas para concretar el requerimiento, ya que el documento de la reunión no los especifica formalmente:

1. **Valores monetarios y origen de las cifras:**
   * Las notas de la reunión **no establecen montos específicos**; únicamente señalan IVA del 19% y valores en pesos sin centavos.
   * Las cifras **\$416.500 propuesto y \$357.000 autorizado pertenecen al ejemplo numérico propuesto en el documento de análisis** (1 repuesto de frenos \$200.000, 1 mano de obra \$100.000 y 1 plumillas \$50.000), utilizado como escenario de prueba y verificación matemática.
   * Se asume como supuesto que el valor registrado por el taller corresponde al **precio unitario antes de IVA**.

2. **Cómputo de la vigencia de 48 horas:**
   * Se asume como supuesto que el plazo de 48 horas continuas inicia en el momento exacto en que el taller publica la cotización adicional (`PublishedAtUtc`).
   * Sábados y domingos cuentan.
   * Vencido el plazo, se bloquea la respuesta a la cotización, pero el cliente conserva el acceso para consultar el avance del vehículo.
   * Una respuesta ya registrada dentro del plazo permanece como `Respondida` y no muta a `Vencida`.

3. **Cálculo de IVA y Redondeo:**
   * Moneda: Pesos Colombianos (COP) enteros.
   * Cálculo por línea con política `MidpointRounding.AwayFromZero`:
     $$\text{Base} = \text{Redondear}(\text{Cantidad} \times \text{PrecioUnitario})$$
     $$\text{IVA} = \text{Redondear}(\text{Base} \times 0.19)$$
     $$\text{Total Ítem} = \text{Base} + \text{IVA}$$
   * Resumen económico: se diferencian el total propuesto y el «Total de adicionales autorizados» (no «Total de la reparación»).

4. **Respuesta Única y Completa:**
   * Se asume como supuesto provisional que el cliente responde todos los ítems en un único envío definitivo por cotización.

5. **Pautas de Usabilidad y Accesibilidad:**
   * Se adoptaron pautas de diseño accesible para móviles inspiradas en buenas prácticas (texto base de ~17-18px, botones táctiles $\ge 44 \times 44$ px y contraste contrastado superior a 4.5:1 en textos principales).
   * **Aclaración de auditoría:** No se declara conformidad formal certificada WCAG AA completa, ya que ello exigiría auditorías externas de compatibilidad integral con lectores de pantalla, flujo de foco exhaustivo y certificación formal.
