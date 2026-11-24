# Coral Rescue — instrucciones para agentes

## Alcance de cambios

- Edita únicamente archivos fuente `.cs` bajo `Assets/Game/Scripts/`. No crees, edites ni generes archivos `.meta`, escenas, prefabs, assets, materiales, shaders, Shader Graph, VFX Graph, sistemas de partículas, configuraciones de Unity ni archivos de proyecto.
- El VFX y el arte técnico los hace el usuario. Tu trabajo es exponer puntos de enganche (eventos C# y parámetros serializados) para que él conecte efectos. Nunca implementes efectos visuales ni toques el look del juego.
- Si una tarea requiere crear objetos, componentes en escena, prefabs o instancias de ScriptableObject (assets), no los modifiques: entrega instrucciones manuales, concretas y ordenadas. Las clases C# de los ScriptableObject sí las puedes escribir.
- Puedes mantener `AGENTS.md` y `AI-CONTEXT.md` cuando cambien estas reglas o el mapa estable del proyecto; no los uses como bitácora de cada tarea.

## Idioma

- Responde en español.
- Todo el código va en inglés: nombres de clases, variables y métodos, comentarios, logs y mensajes de error.

## Presupuesto de contexto

- Máximo 3 archivos y ~300 líneas por tarea de ejecución. En fase de planificación, máximo 5 archivos y ~500 líneas. Si necesitas más, detente, di qué falta y espera confirmación.
- Orden obligatorio: `grep` del símbolo → `read` del rango exacto → editar. No enumeres directorios ni uses `glob` sobre todo `Assets/`.
- Prohibido leer `*.meta`, `*.prefab`, `*.unity`, `*.asset`, `*.mat`, `*.shader`, `*.shadergraph`, `*.vfx`, `*.csproj`, `*.sln`, `*.slnx`, `Library/`, `Temp/`, `Logs/`, `obj/`, `ProjectSettings/` y `UserSettings/`.
- READMEs y documentación: solo la sección relevante (`grep -C 10 "<tema>" <archivo>`), nunca el archivo completo.
- Consulta `AI-CONTEXT.md` solo si la tarea cruza áreas o necesitas ubicar un módulo; para cambios locales, empieza por el código.
- No releas un archivo ya leído en la sesión salvo que haya cambiado o que el paso lo exija.
- No "verifiques" leyendo módulos que no estén implicados en la tarea.
- Antes de crear lógica nueva, busca el patrón existente más cercano y reutilízalo.
- Trabaja directamente: no delegues ni inicies subagentes salvo petición explícita.
- No uses herramientas de la CLI o del MCP de Unity salvo que el usuario lo pida.

## Convenciones de código

- Separa simulación de presentación: los scripts de lógica no referencian VFX, audio ni UI. Emiten eventos (`event Action<...>`) y la presentación se suscribe.
- Los valores de diseño (daño, radio, alcance, peso, capacidad, tiempos) viven en ScriptableObjects o en campos `[SerializeField]`, nunca como números mágicos en el código.
- Campos serializados privados (`[SerializeField] private`), referencias cacheadas en `Awake`/`Start`, sin `Find*` ni `GetComponent` en `Update`.
- Una responsabilidad por MonoBehaviour, pequeño y sin dependencias circulares entre módulos.
- Namespace `Game.<Modulo>` (por ejemplo `Game.Cleaning`).
- No añadas paquetes ni dependencias nuevas sin que el usuario lo pida.
- Si hay que elegir entre una solución simple y una flexible, elige la simple: es una game jam con plazo corto.

## Flujo de trabajo: planificar antes de ejecutar

Por defecto trabaja en dos fases y **no edites código en la primera**.

### Fase 1 — Plan

1. Localiza la arquitectura mínima: módulo, interfaces y eventos implicados, dentro del presupuesto de contexto.
2. Devuelve el plan con este formato:
   - **Entendimiento**: 2–3 líneas con el diseño propuesto y las decisiones clave.
   - **Archivos**: rutas a crear o modificar y por qué.
   - **Pasos**: lista numerada; cada paso indica archivo, cambio y verificación.
   - **Manual en Unity**: checklist de lo que hace el usuario.
   - **Puntos de enganche para VFX**: eventos o parámetros que expone el cambio.
   - **Riesgos / preguntas**: solo lo que bloquee la implementación.
3. Termina pidiendo aprobación. No edites archivos en esta fase.

### Fase 2 — Ejecución

- Implementa solo los pasos aprobados, en orden, sin volver a explorar lo ya revisado.
- Si un paso revela un bloqueo, detente al terminar ese paso y explica la desviación.
- Si el plan supera 6 pasos, propón dividirlo en fases y ejecuta solo la primera.

### Tareas mecánicas

Si el usuario ya dio archivo, símbolo y comportamiento concretos, salta la Fase 1 e implementa directamente.

## Contrato de respuesta

- Implementa con `edit`; no pegues el archivo completo en el chat.
- Explicación máxima de 5 líneas: qué cambió y por qué.
- Pasos manuales de Unity: checklist numerada y breve, solo si son imprescindibles.
- No resumas lo que el diff ya muestra, no repitas el enunciado y no propongas refactors, tests ni mejoras fuera del alcance.

## Validación

- Compila solo el proyecto afectado y filtra la salida:
  `dotnet build Assembly-CSharp.csproj -v q --nologo 2>&1 | Select-String "error CS|warning CS"`
- No compiles la solución completa ni abras el Editor de Unity.
- Si el `.csproj` no existe o no compila fuera de Unity (Unity lo genera al abrir el proyecto), dilo en una línea y no insistas: el usuario revisará la consola del Editor.
- Reporta en una línea qué no se pudo verificar sin Editor.

Al terminar, resume los archivos C# modificados, la validación realizada y cualquier paso manual pendiente en Unity.
