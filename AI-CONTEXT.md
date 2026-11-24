# Coral Rescue — contexto breve del proyecto

Juego de game jam en Unity 6.3 LTS (6000.3.24f1), primera persona, low poly, ambientación inspirada en Subnautica. La cámara es estática y rota 360° con el cursor; el personaje no camina (estilo Virtua Cop). El jugador protege un arrecife de coral: recoge basura, limpia y caza peces con tres armas, y sobrevive hasta que se acabe el tiempo (estilo Five Nights at Freddy's).

## Mapa

Estructura propuesta; ajústala a la real y borra lo que no exista.

- `Assets/Game/Scripts/Core/`: eventos, interfaces y tipos compartidos.
- `Assets/Game/Scripts/Player/`: cámara estática con rotación de 360° y apuntado con el cursor.
- `Assets/Game/Scripts/Weapons/`: rueda de armas y las tres armas (recolectar, limpiar, cazar).
- `Assets/Game/Scripts/Reef/`: salud del arrecife y obligaciones del jugador (basura, suciedad, peces).
- `Assets/Game/Scripts/Enemies/`: pulpo, puntos débiles y mancha de tinta.
- `Assets/Game/Scripts/Radar/`: registro de entidades rastreables y datos del radar.
- `Assets/Game/Scripts/Flow/`: niveles, temporizador de supervivencia, victoria y derrota.
- `Assets/Game/Scripts/UI/`: HUD (solo presentación; se suscribe a eventos).
- `Assets/Game/Data/`: instancias de ScriptableObject (armas, tipos de basura, peces, pulpo, niveles). Las crea el usuario.
- `Assets/Game/VFX/` y `Assets/Game/Art/`: shaders, partículas, materiales y arte. Son del usuario; el agente no los lee ni los edita.
- `Assets/Scenes/`: escenas de Unity; el agente no las edita. Cualquier creación o configuración en ellas corresponde al usuario en el Editor.

## Orientación de arquitectura

- Simulación y presentación van separadas. La lógica calcula estado y emite eventos C#; los efectos visuales, el audio y la UI se suscriben y nunca deciden reglas del juego.
- Los datos de diseño viven en ScriptableObjects (daño, castigo, tiempos, pesos, duración de la partida), no en el código.
- Los módulos se comunican por eventos e interfaces pequeñas, sin referencias circulares entre carpetas.
- Evita singletons estáticos nuevos. Si hace falta un punto de acceso global, mantenlo pequeño y en `Core`.

## Mecánicas

Lo marcado como pendiente se acuerda en el plan antes de implementarlo.

- **Cámara:** estática, el jugador no se desplaza. Cursor libre con rotación en los bordes: al acercar el cursor a un borde de la pantalla la cámara rota hacia ese lado, con zona de borde y velocidad configurables. Rota 360° en horizontal. Los límites verticales son mínimos o casi nulos para no marear al jugador: valor inicial pequeño y ajustable en el Inspector, a calibrar probando el juego.
- **Armas y rueda:** tres armas (recolectar, limpiar, cazar) elegidas con una rueda estilo GTA V. Cada arma implementa una interfaz común (`IWeapon`) y la rueda solo cambia el arma activa y emite `WeaponChanged`. Mientras la rueda está abierta el tiempo se ralentiza (factor configurable, no se pausa) y la UI de la rueda usa tiempo sin escalar. Eventos `WheelOpened` y `WheelClosed`.
- **Obligaciones con castigo:** basura, suciedad y peces son obligaciones que, si no se resuelven, quitan vida. Se modelan con un patrón común: estado pendiente, resuelta o fallida, con un castigo único al fallar y un daño por segundo mientras siga pendiente. Ambos valores son configurables en ScriptableObject (cero si no aplica), y el daño por segundo puede crecer con el tiempo que lleve pendiente mediante una curva configurable (lo usa el pulpo). Así el agente no duplica lógica y el equipo equilibra la dificultad sin tocar código. Con varias obligaciones activas a la vez el daño se acumula: el equilibrio se ajusta en los datos, y puede añadirse un tope de daño simultáneo.
  - **Basura:** cae de forma aleatoria desde arriba y alrededor del jugador, con objetos de distintos tipos y a velocidad lenta para dar tiempo a reaccionar. Estados: cayendo, en el fondo, recogida. Si se recoge con el arma de recolectar antes de tocar el fondo, no hay castigo. Si toca el fondo, se aplica el castigo único, la basura se queda en el suelo y resta salud poco a poco mientras siga ahí, para obligar al jugador a vigilar su entorno. Sigue siendo recogible y recogerla también cuenta para el contador. Eventos: `TrashSpawned`, `TrashLanded`, `TrashCollected`.
  - **Suciedad del arrecife:** el arrecife se divide en zonas de coral. Cada zona tiene tres estados: limpia, ensuciándose y sucia. Mientras una zona esté ensuciándose o sucia resta salud poco a poco, hasta que se limpie con el arma de limpiar. Cada zona guarda un progreso de suciedad de 0 a 1 y emite `CoralDirtyingStarted`, `CleaningStarted`, `CleaningProgressed(float)` y `Cleaned`. Al empezar a ensuciarse se emite un aviso al jugador (`AlertRaised`) con la zona afectada, al estilo de la caja de música de FNAF. Una zona empieza a ensuciarse por una fuente de suciedad: en los niveles 2 y 3, un vertido de petróleo; en el nivel 4, la tinta del pulpo.
  - **Peces:** peces dañinos para el arrecife (por ejemplo una especie invasora) aparecen y nadan por la zona. No escapan: siguen en escena hasta que se atrapan. Mientras estén vivos restan salud poco a poco, igual que la suciedad, y el daño se detiene al atraparlos con el arma de cazar. Sin castigo único salvo que se configure. Eventos: `FishSpawned`, `FishCaught`.
- **Salud:** una sola barra compartida: la vida del jugador y la "Salud del Arrecife" son lo mismo. Eventos `HealthChanged` y `Depleted`. Todos los castigos pasan por un único punto de entrada de daño.
- **Munición y recarga:** sustituye al inventario por peso. La recolección no guarda objetos en una mochila; solo cuenta basura recogida (12/50 en el concepto). El indicador de capacidad del HUD muestra la munición del arma activa. Recarga con una tecla o con el botón derecho del mouse, no disparando fuera de pantalla, porque el borde de la pantalla rota la cámara. Cada arma se configura en su propio ScriptableObject (tamaño del cargador o tanque, tiempo de recarga, tiempo de espera entre usos) y emite `AmmoChanged`, `ReloadStarted`, `ReloadFinished` y `EmptyTriggered`.
  - **Recolectar:** sin munición, solo un tiempo de espera corto entre usos.
  - **Limpiar:** tanque que se vacía mientras se limpia y se rellena al recargar.
  - **Cazar:** cargador pequeño con recarga más lenta.
- **Pulpo:** obstáculo extra del nivel 4; el nivel se gana sobreviviendo, no derrotándolo. Aparece y, mientras se queda, resta salud con un daño por segundo que crece con el tiempo que lleve presente (curva configurable en su ScriptableObject). Para alejarlo hay que apuntar a sus puntos débiles: acertar suficientes veces lo hace retirarse y el daño se detiene. Lanza tinta de dos formas: sobre la pantalla del jugador, que tapa la vista y se limpia con el arma de limpiar, y sobre zonas de coral, que pasan a ensuciándose y activan el aviso. La lógica solo guarda estado (cuánta tinta queda en la pantalla y en cada zona, cuántos aciertos lleva); cómo se ve la mancha en pantalla es VFX/UI del usuario. Eventos: `OctopusAppeared`, `WeakPointHit`, `OctopusRetreated`, `InkSplashed(target)` (pantalla o zona de coral) e `InkCleaned`. Aparece varias veces durante el nivel 4, con pausas entre apariciones configurables en el `LevelConfig`.
- **Radar:** muestra a los enemigos en rojo y las zonas de coral: verde si está limpia y naranja si se está ensuciando. Va junto al aviso en pantalla ("un coral está siendo ensuciado, ve a limpiarlo"). Se alimenta de un registro de entidades rastreables con tipo y estado; el radar solo lee ese registro y los cambios de estado se notifican por eventos.
- **Flujo de partida:** temporizador de supervivencia por nivel. Se gana el nivel si la salud sigue por encima de cero cuando el tiempo termina; se pierde si llega a cero antes. Eventos: `LevelStarted`, `LevelWon`, `LevelLost`. Las condiciones de cada nivel están en la sección Niveles.

## Niveles

Cuatro niveles, cada uno suma una mecánica. Cada nivel se define en un `LevelConfig` (ScriptableObject) con la duración, qué obligaciones están activas y sus ritmos de aparición y valores de daño. El código no cambia entre niveles: solo cambia el `LevelConfig`.

1. Basura que cae.
2. Limpieza y basura.
3. Peces, limpieza y basura.
4. Todo junto más el pulpo como obstáculo extra: aparece y hay que apuntar a sus puntos débiles para alejarlo cuanto antes. Nivel final.

- Niveles 1 a 3: se ganan sobreviviendo hasta que termine el tiempo con la salud por encima de cero.
- Nivel 4: también se gana sobreviviendo hasta que termine el tiempo. El pulpo no es una condición de victoria, sino un obstáculo extra; si la salud llega a cero antes, se pierde.
- Fuente de suciedad: en los niveles 2 y 3, vertidos de petróleo al mar. Es un evento configurado en el `LevelConfig` (tiempo entre vertidos, zonas posibles, intensidad) que cae sobre una zona de coral al azar y emite `OilSpillStarted(zone)`. En el nivel 4 la fuente es la tinta del pulpo. Ambas llevan al mismo evento `CoralDirtyingStarted`, así que la limpieza, el aviso y el radar no cambian según la fuente.

## Eventos para VFX

Cualquier mecánica nueva debe exponer sus eventos de gameplay (acción iniciada, progreso, resultado, fallo) para que el usuario cuelgue efectos sin tocar la lógica. Al planificar, lista los eventos que añade el cambio.

## Ruta para una tarea de código

1. Identifica el módulo o símbolo directamente relacionado y busca únicamente en sus archivos `.cs`.
2. Lee el archivo afectado y las dependencias inmediatas necesarias.
3. Implementa el cambio en C# sin modificar archivos generados ni recursos serializados de Unity.
4. Ejecuta la validación enfocada descrita en `AGENTS.md` y reporta lo que no pueda verificarse sin Unity.
5. Si hacen falta objetos, prefabs, assets o cambios de escena, detente antes de editar recursos y da al usuario pasos manuales claros.

Actualiza este mapa solo cuando cambie la estructura o una convención estable; no registres aquí el historial de cada tarea.
