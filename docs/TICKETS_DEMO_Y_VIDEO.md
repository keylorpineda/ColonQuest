# Tickets: demo y video por perfil

Listos para pegar en Jira (proyecto SCRUM). Origen: [DEMO_Y_VIDEO_POR_PERFIL.md](DEMO_Y_VIDEO_POR_PERFIL.md)
y [INVESTIGACION_EVALUADORES_Y_GANADORES.md](INVESTIGACION_EVALUADORES_Y_GANADORES.md).
Código congelado el 2 de nov a las 11:59 p. m.; entrega oficial el 4 de nov, 8:00 p. m. hora de Costa Rica.

Convención: título del PR `tipo(alcance): resumen [SCRUM-n]`, rama con el número del ticket.

## Prioridad 1: que funcione en vivo

### 1. Verificar el link público del build web (Jefferson y Keylor)
- Abrir el link en incógnito, en móvil y en PC (Chrome, Edge, Firefox) sin login ni errores de consola.
- Carga en menos de 8 s en una conexión normal.
- El micrófono pide permiso de forma limpia y el texto sirve de respaldo.
- Hecho cuando: la lista de pruebas queda en el ticket con captura por navegador.

### 2. Mantener despierto el backend (Keylor)
- Monitor externo que llame a `GET /api/ping` cada 8 minutos hasta el 9 de nov.
- Hecho cuando: una llamada fría tras 30 minutos sin tráfico responde en menos de 3 s.

### 3. Créditos de Meta frente al juicio (Keylor)
- Confirmar cuándo vencen los $150 (ventana de 10 días) frente al juicio del 4 al 8 de nov.
- Si vencen antes: llave secundaria en el backend y respaldo a contenido local verificado.
- Hecho cuando: el juego completa un mes sin llamadas a Meta y sin pantallas de error.

### 4. Indicador Live / Offline y respaldo a 8 s (Jefferson)
- Chip discreto que muestra si la respuesta viene del modelo o del respaldo local.
- Si el servidor tarda más de 8 s, entra el respaldo sin congelar ni mostrar error.
- Hecho cuando: con el backend apagado, el mes completo se juega y el chip dice Offline.

## Prioridad 2: Judge Run y el momento wow

### 5. Botón "Play in 90 seconds" y ruta guiada (Jefferson)
- Botón en el menú que lleva a la ruta: salario, evento, vendedor, consecuencia, cierre.
- Sin cuenta, sin tutorial, en inglés por defecto.
- Hecho cuando: un tester nuevo llega al cierre del mes en 90 s o menos, sin ayuda.

### 6. Preguntas sugeridas en la negociación (Jefferson)
- Tres preguntas en pantalla (por ejemplo: costo total, seguros, comisión) que se pueden tocar o hablar.
- Siempre visible la entrada de texto junto al botón de voz.
- Hecho cuando: una persona que no sabe qué preguntar revela al menos un hecho escondido.

### 7. Revelación de la ficha oculta con barra de costo real (Jefferson)
- La ficha se abre línea por línea; el precio de contado se tacha; la cuota real sube en una barra "lo prometido vs el costo real".
- Las cifras vienen del motor, nunca del modelo.
- Hecho cuando: el momento se entiende en menos de 10 s sin explicación, y se graba un clip de prueba.

### 8. Tarjeta de cierre del mes (Jefferson)
- Muestra el concepto aprendido y qué preguntas clave hizo bien el jugador.
- Dos botones: "How teachers see this" (Modo Clase) y "Under the hood" (Demo Lab).
- Hecho cuando: ambos botones llevan a su pantalla y se puede volver.

## Prioridad 3: evidencia para Meta e impacto

### 9. Datos de evidencia por llamada en la respuesta (Keylor)
- Cada respuesta del backend lleva modelo, proveedor, latencia, costo y herramientas usadas, en el contrato de `shared/`.
- Hecho cuando: el cliente puede leerlos sin una llamada extra y el esquema OpenAPI está actualizado.

### 10. Chips de evidencia dentro de la escena (Jefferson)
- Chip sobre el personaje: "Muse Spark Open, 2,1 s, $0,002", que se despliega al rastro de herramientas.
- Hecho cuando: se ve en el evento del game master y en el vendedor sin salir de la escena.

### 11. Tarjeta del piloto y Modo Clase con datos reales (Keylor)
- N y porcentajes agregados, sin nombres ni rostros; si no hay datos, mostrar la clase simulada rotulada como tal.
- Hecho cuando: la pantalla del Modo Clase muestra cifras calculadas con SQL y la explicación redactada por Muse Spark.

## Prioridad 4: entrega

### 12. Guion y grabación del video de 2:30 (Jefferson y Keylor)
- Gancho completo en los primeros 10 s; arquitectura y piloto después; todo lo esencial antes del segundo 30.
- Inglés o español con subtítulos en inglés, 1080p. Corte vertical de 30 a 45 s para X.
- Hecho cuando: el video se revisa con una persona que no conoce el proyecto y entiende qué es en 10 s.

### 13. README y texto de envío con el encuadre ganador (Jefferson y Keylor)
- Primera frase: un simulador de finanzas personales con IA para manejar un salario durante el mes y evitar estafas, con el dato de la OCF (87% con deudas, 34% del ingreso para pagarlas).
- Declarar los cinco modelos de Meta, Muse Spark Open en Baseten y el control de costos.
- Hecho cuando: el texto de envío está listo antes del 3 de nov y el link del README abre.

### 14. Corregir el README (Jefferson y Keylor)
- Primera frase con el encuadre de simulador de finanzas personales y el dato de la OCF (87% con deudas, 34% del ingreso para pagarlas).
- Enlaces a la demo y al video en las primeras líneas.
- La frase "a student pilot" solo se queda si `docs/pilot/` tiene cifras agregadas.
- Hecho cuando: todos los enlaces del README abren y cada afirmación tiene respaldo en el repositorio.

### 15. Motor: quitar el `double` de la cuota (Keylor, opcional)
- Reemplazar `Math.Pow` por una potencia en `decimal` en `FinanceEngine.Obligations.cs`.
- Documentar que los créditos fiscales (hijos, cónyuge) no se modelan.
- Hecho cuando: `Mandatory2` y la suite de `dotnet test` siguen en verde.

### 16. Actualizar CLAUDE.md (Keylor)
- Plazo oficial del 4 de nov, 9:00 PM ET; premios extra marcados como sin confirmar; ventana de 10 días de los créditos; encuadre de simulador.
- Hecho cuando: ninguna fecha ni premio contradice el texto oficial del evento.

### 17. Ruta de 90 s publicada, verificada y, si da tiempo, autojugada (Jefferson y Keylor)
- La guía está en [RUTA_DEMO_90S.md](RUTA_DEMO_90S.md). Jugarla una vez completa y corregir lo que no coincida.
- Opcional: modo que reproduce la ruta sola, sin que el evaluador toque nada.
- Hecho cuando: una persona que no conoce el proyecto la recorre en menos de 2 minutos, sin ayuda.

### 18. Build descargable como Release de GitHub (Jefferson)
- Zip de Windows y de Mac, suma SHA-256 en la descripción, paso de ejecución al inicio del README y aviso del sistema (SmartScreen y Gatekeeper).
- No guardar el ejecutable dentro del repositorio.
- Hecho cuando: se descarga y corre en una máquina limpia sin Unity, y no lleva ninguna llave de API.

## Si falta tiempo se recorta en este orden

1. Chips desplegables del ticket 10 (queda el chip simple).
2. Sonidos extra y sacudida de cámara.
3. Corte vertical para X.
4. Arquitectura animada (queda un diagrama estático).

No se recortan: 1, 2, 3, 4, 5, 6, 7 y 12.
