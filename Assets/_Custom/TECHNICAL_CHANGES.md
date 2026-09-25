# Documento Técnico — Cambios Realizados y Pendientes

**Proyecto:** Instant-Unity

**Fecha:** 18 de junio de 2026

**Estado:** Implementación de código completada. Compilación exitosa. Falta conexión de componentes en el editor de Unity.

---

## 1. Resumen ejecutivo

Se implementó en código la mayoría de los elementos faltantes del documento de diseño para cumplir con las fases 2, 3 y 4 del proyecto:

- **Fase 2 (Game feel y gameplay completo):** partículas, hit flash, glow de élite, reloj pulsante, dash trail, Damage Numbers Pro, ventana de upgrade con pausa parcial y timeout.
- **Fase 3 (Progresión y persistencia):** SkinManager, SkinRenderer, onboarding con tooltips, HapticManager.
- **Fase 4 (Monetización):** AdsManager con stub de Editor y AdMob para anuncios recompensados, pantalla de Game Over con botón de anuncio.

El código compila sin errores. Persisten conexiones de HUD, audio secundario, partículas de ganancia de tiempo y trail de dash por completar.

---

## 2. Managers modificados

### 2.1 TimeManager.cs

**Cambios:**

- Se respeta el tope máximo de tiempo de 45 segundos (`TIME_MAX`).
- `AddTime` y `SubtractTime` usan `Mathf.Clamp` para mantener el rango `[0, 45]`.
- Se añadió `SetDrainMultiplier(float)` para permitir pausa parcial (20% de drenaje) durante ventanas de upgrade.
- Se añadió evento `OnTimeColorChanged` con estados `Calm`, `Warning` y `Danger`.
- Se añadió evento `OnTimeCriticalEnded`.
- Se implementó reloj pulsante automático cuando `CurrentTime <= 5s` (llama a `AudioManager.PlayClockBeep`).
- Se añadió `ResetTime()` para reinicios de partida.

**Estado:**

- `clockBeepSFX` está asignado temporalmente desde NiceVibrations.

### 2.2 SpawnManager.cs

**Cambios:**

- Intervalo de élite cambiado a 45 segundos (`ELITE_INTERVAL = 45f`).
- El spawn se pausa completamente durante ventanas de upgrade (`GameState.Upgrade`).
- Se añadió `ResetGameTime()` para reinicios de partida.

### 2.3 EnemyManager.cs

**Cambios:**

- Se añadió `KillsSinceLastUpgrade` para contar kills desde el último upgrade.
- Se añadió evento `OnKillsThresholdReached` que se dispara al alcanzar el umbral dinámico de kills (10 al inicio, +4 por ventana hasta un máximo de 38).
- Se añadieron `ResetKillCount()` y `ResetKillsSinceLastUpgrade()`.

### 2.4 UpgradeManager.cs

**Cambios:**

- Se implementó upgrade común al alcanzar el umbral dinámico de kills (escucha `EnemyManager.OnKillsThresholdReached`).
- Se implementó upgrade raro garantizado al matar élite (escucha `EnemyManager.OnEnemyKilled` con `isElite`).
- La ventana de upgrade ahora usa pausa parcial (`TimeManager.SetDrainMultiplier(0.2f)`).
- Se añadió timeout de 8 segundos con barra de progreso (`OnUpgradeTimerChanged`).
- Se añadió evento `OnUpgradeWindowClosed`.
- Se añadió `ResetUpgrades()` para reinicios de partida.
- Al cerrar la ventana se restaura música al 100% con fade.
- Si el tiempo se agota sin elegir, se reproduce `upgradeMissedSFX`.

**Estado:**

- Las listas comunes y raras, y `upgradeMissedSFX`, están asignados en `1_Game`.

### 2.5 GameManager.cs

**Cambios:**

- Se hizo público `ChangeState(GameState)`.
- Se añadió `ChangeToUpgradeState()`.
- Se añadió evento `OnGameOver(float time, int kills, int cronos)`.
- Se añadió evento `OnGameRestarted`.
- Se añadió `RestartGame()` para reiniciar partida sin recargar escena.
- Se añadió `ResetGameSystems()` para reiniciar tiempo, kills, spawn y upgrades.
- `TriggerGameOver` detiene la música y guarda `FirstTimePlayed`.

### 2.6 SaveManager.cs

**Cambios:**

- Se añadió `CurrentRunCronos` para mostrar Cronos ganados en la partida actual.
- Se añadió `VibrationEnabled` y métodos `SetVibration()`.
- Se añadió `IsFirstTime()` y `SetFirstTimePlayed()` para onboarding.
- Se añadió `EquippedEnemySet` y `EquipEnemySet()`.
- Se añadieron métodos para gestionar cronos de partida.
- `LoadData` y `SaveData` ahora persisten todos los nuevos campos.

### 2.7 AudioManager.cs

**Cambios:**

- Se añadieron clips `upgradeMissedSFX`, `timeGainSFX`, `clockBeepSFX`.
- Se añadió `PlayClockBeep()`.
- Se añadió `PlayTimeGainSFX()`.
- Se añadió `StopMusic()`.
- Se mantiene `FadeMusicTo(float, float)`.

**Estado:**

- Música, tensión y sonidos secundarios están asignados temporalmente desde NiceVibrations.

---

## 3. Nuevos managers

### 3.1 HapticManager.cs

- Singleton.
- `TriggerDamage()`: vibración fuerte de 80ms.
- `TriggerEliteKill()`: vibración media de 40ms.
- Respeta el toggle `SaveManager.VibrationEnabled`.
- Usa `Handheld.Vibrate()` para Android/iOS.

### 3.2 AdsManager.cs

- Singleton.
- Stub de Editor y carga real de AdMob en Android/iOS.
- `ShowRewardedAd(Action onRewardGranted)` usa el stub en Editor y recompensa mediante callback en dispositivo.
- `IsAdReady` indica si hay anuncio disponible.

**Pendiente:**

- Sustituir IDs de prueba y configurar unidades reales antes de publicar.

### 3.3 SkinManager.cs

- Singleton.
- Maneja `SkinTarget.Player` y `SkinTarget.EnemySet`.
- Eventos `OnPlayerSkinChanged` y `OnEnemySetChanged`.
- Métodos `EquipSkin`, `GetEquippedSkin`, `IsSkinUnlocked`.

### 3.4 BootstrapInitializer.cs

- Crea automáticamente todos los managers si no existen en la escena.
- Soporta `DontDestroyOnLoad`.
- Opcionalmente carga la escena del menú principal al inicio.

### 3.5 ParticleManager.cs

- Singleton.
- Pools para partículas de muerte, tiempo ganado y trail del dash.
- `SpawnDeathParticles(Vector3, Color, int)`: burst pooleado con color del enemigo, variación radial, fade y límite de partículas activas.
- `SpawnTimeGainParticles(Vector3, int)`: partículas verdes ascendentes.
- `SpawnDashTrail(Vector3, Vector2, float)`: rectángulo azul semitransparente.

**Estado:**

- Los prefabs de muerte, ganancia de tiempo y trail de dash están creados y asignados.

### 3.6 DamageNumbersManager.cs

- Usa `DamageNumberGUI` en coordenadas de pantalla para el daño jugador→enemigo y enemigo→jugador, convertido desde la posición del mundo por la cámara 2D.
- Usa `DamageNumberGUI` para mostrar cada ajuste explícito del reloj junto al HUD.
- Configura pooling y prewarm de las variantes propias sin modificar el contenido del asset.
- Escucha `TimeManager.OnTimeAdjusted` para mostrar el delta efectivo después del clamp.

---

## 4. Entidades

### 4.1 EnemyVisualFeedback.cs

- `TriggerHitFlash()`: flash blanco de 100ms.
- `SetEliteGlow(bool)`: halo dorado pulsante (#FFAA0055) con escala 1.0-1.15 cada 0.8s.
- `SetBaseColor(Color)`: establece el color base del enemigo.

### 4.2 ToxicZone.cs

- Área circular semitransparente naranja (#FF660055).
- Borde punteado que rota lentamente.
- Daña al jugador mientras permanezca dentro (`timeDamagePerSecond`).

### 4.3 GeometryRenderer.cs

- Genera figuras geométricas por código: triángulo, círculo, diamante, cuadrado, hexágono.
- Soporta color y borde.

### 4.4 SkinRenderer.cs

- Componente adjunto a Player y enemigos.
- Por defecto activa el renderer geométrico.
- Cuando hay un sprite asignado, activa el `SpriteRenderer` del skin y desactiva la geometría.
- Escucha eventos de `SkinManager` para actualizar visualmente.

### 4.5 EnemyBase.cs

**Cambios:**

- Integra `EnemyVisualFeedback` automáticamente.
- Aplica color base y glow de élite en `OnEnable`.
- En `Die()`: añade tiempo, reproduce SFX de tiempo ganado, spawnea partículas de muerte y tiempo ganado, y dispara haptic si es élite.
- En `OnHit()`: dispara hit flash.

### 4.6 PlayerMovement.cs

**Cambios:**

- Propiedades públicas: `IsDashing`, `DashCooldownRemaining`, `DashCooldownTotal`, `DashCooldownRatio`.
- Al iniciar dash: reproduce SFX y spawnea trail del dash.
- Usa `SkinRenderer` para aplicar skins si está disponible.

### 4.7 PlayerCombat.cs

**Cambios:**

- Dispara `HapticManager.TriggerDamage()` al recibir daño.

### 4.8 PlayerInput.cs

**Cambios:**

- Ahora busca `JoystickController` y mezcla input del InputSystem con el joystick virtual.
- Se añadió `TriggerDash()` para ser llamado desde UI.

---

## 5. UI

### 5.1 HUDController.cs

- Actualiza reloj, kills y Cronos de la partida.
- Cambia color del reloj según estado: blanco (>15s), amarillo (5-15s), rojo (≤5s).
- Barra de tiempo que se vacía según el tiempo restante.
- Muestra/oculta el HUD según el estado del juego.

### 5.2 GameOverController.cs

- Muestra panel de Game Over con tiempo, kills y Cronos ganados.
- Muestra récords personales.
- Indica si se superó un récord.
- Botón "Ver anuncio" para anuncio recompensado (llama a `AdsManager`).
- Botón "Reiniciar" para nueva partida.
- Botón "Tienda" para volver al menú principal y abrir la tienda.

### 5.3 UpgradeUIManager.cs

**Cambios:**

- Escucha `OnUpgradeWindowClosed` y `OnUpgradeTimerChanged`.
- Muestra texto "ELIGE UN UPGRADE".
- Muestra barra de progreso del mini-cronómetro de 8s.
- La barra cambia a rojo cuando quedan menos de 3 segundos.

### 5.4 TooltipController.cs

- Muestra 3 tooltips en la primera partida:
  - "TU VIDA" apuntando al reloj.
  - "MÁTALOS PARA GANAR TIEMPO" centrado.
  - "DASH — INVULNERABLE" apuntando al botón de dash.
- Pausa el juego ligeramente durante 2 segundos por tooltip.
- Marca `FirstTimePlayed = false` al finalizar.

### 5.5 JoystickController.cs

- Joystick virtual con zona muerta de 8px.
- Radio configurable.
- Fade cuando no se usa.

### 5.6 DashButtonController.cs

- Anillo de cooldown que se llena visualmente.
- Color azul cuando está listo, gris en cooldown.
- Notifica al `TooltipController` cuando el dash está disponible.
- Llama a `PlayerInput.TriggerDash()` al presionar.

### 5.7 MainMenuUI.cs

**Cambios:**

- Añadidos toggles de música, SFX y vibración.
- `ActionSkinClick` ahora usa `SkinManager`.
- Al volver desde Game Over con el botón de tienda, abre la tienda automáticamente.

---

## 6. Estructura de carpetas

Se crearon las carpetas y scripts sugeridos en el documento:

```text
Assets/_Custom/
├── Scripts/
│   ├── Managers/
│   │   ├── AdsManager.cs
│   │   ├── HapticManager.cs
│   │   ├── SkinManager.cs
│   │   ├── DamageNumbersManager.cs
│   │   └── BootstrapInitializer.cs
│   ├── Rendering/
│   │   ├── GeometryRenderer.cs
│   │   └── SkinRenderer.cs
│   ├── Utils/
│   │   ├── ParticleManager.cs
│   │   └── ObjectPooler.cs
│   ├── UI/
│   │   ├── HUDController.cs
│   │   ├── GameOverController.cs
│   │   ├── TooltipController.cs
│   │   ├── JoystickController.cs
│   │   └── DashButtonController.cs
│   └── Enemies/
│       ├── EnemyVisualFeedback.cs
│       └── ToxicZone.cs
├── INTEGRATION_NOTES.md
└── TECHNICAL_CHANGES.md
```

---

## 7. Verificación de compilación

Se ejecutó `dotnet build Assembly-CSharp.csproj` exitosamente:

- Sin errores.
- Sin warnings.

---

## 8. Pendientes de conexión en Unity

### 8.1 Managers y componentes

- Los managers necesarios existen en la escena de juego; `BootstrapInitializer` detecta componentes existentes.
- `UpgradeManager`: listas comunes y raras asignadas.
- `AudioManager`: clips temporales asignados; las fuentes se crean automáticamente.
- `SaveManager`: se crea de forma persistente desde el menú o `GameManager`; no añadirlo al objeto `Managers` de `1_Game`.
- `ParticleManager`: los tres prefabs están asignados.

### 8.2 UI en escena de juego

- `HUDController`: barra, tiempo, kills, Cronos y raíz del HUD asignados.
- `GameOverController`: estadísticas detalladas y botones asignados.
- `UpgradeUIManager`: título, barra de timeout y panel asignados.
- `TooltipController`: conectar `tooltipPanel`, `tooltipText`, `timeTextTarget`, `dashButtonTarget`.
- `JoystickController`: conectar fondo y handle.
- `DashButtonController`: conectar `cooldownRing`, `buttonImage`.

### 8.3 Player

- Añadir `SkinRenderer` al prefab del jugador.
- Verificar que `PlayerInput` y `PlayerMovement` estén en el mismo objeto.

### 8.4 Enemigos

- `EnemyVisualFeedback` ya está añadido a cada prefab de enemigo.
- Configurar `baseColor` e `isElite` en `EnemyBase`.

### 8.5 Menú principal

- `MainMenuUI`: conectar `musicToggle`, `sfxToggle`, `vibrationToggle`.

### 8.6 Audio

- Todos los clips de prototipo están asignados desde NiceVibrations.
- Asignar volumen general: música 0.6, SFX 0.8, daño 1.0, beep 0.4-1.0.

### 8.7 Prefabs de partículas

Prefabs creados:

- `deathParticlePrefab`: `Assets/_Custom/Prefabs/VFX/EnemyDeathParticle.prefab`.
- `projectilePrefab`: `Assets/_Custom/Prefabs/Combat/EnemyProjectile.prefab`.

- `timeGainParticlePrefab`: figura pequeña verde.
- `dashTrailPrefab`: rectángulo azul semitransparente.

### 8.8 Integración de anuncios

- Configurar IDs reales de AdMob y unidades de anuncios.

---

## 9. Notas técnicas adicionales

- El juego usa `UnityEngine.Pool.ObjectPool<T>` para enemigos, proyectiles y partículas, cumpliendo con el requisito de object pooling.
- La comunicación entre managers se mantiene mediante eventos `Action` y `Action<T>`.
- Todos los managers son Singletons.
- Los upgrades obtenidos durante una partida **no persisten** entre partidas (cada partida empieza desde cero).
- La skin del jugador y el set de enemigos persisten en `PlayerPrefs`.

---

## 10. Próximos pasos recomendados

1. Probar el flujo completo: menú → progresión → juego → upgrade → Game Over → anuncio/reinicio.
2. Validar HUD y safe area en resoluciones landscape con notch.
3. Ajustar el balance de tiempos, spawn y dificultad según métricas reales.
4. Sustituir los audios temporales por contenido con licencia comercial.
5. Configurar IDs y unidades reales de AdMob antes de publicar.

---

## 11. Alineación con el GDD (24 de septiembre de 2026)

Se mantienen las desviaciones de diseño del proyecto (drenaje 1.25, penalización 6s, orden tanque→tirador, landscape, auto-elección al agotar la ventana, revivir por anuncio, progresión permanente).

**Bugs corregidos**
- El élite ya no se salta con la arena llena (`SpawnManager.SpawnElite`).
- `AudioManager.FadeMusicTo` es relativo al volumen del jugador (antes subía la música a 1.0 tras cada upgrade).
- El HUD no pone los Cronos a 0 ni se oculta durante la ventana de upgrade / pausa de tooltips.
- SFX de golpe (`impactSFX`), daño recibido (`playerHurtSFX`), muerte de élite (`eliteDeathSFX`) y upgrade disponible (`upgradeAvailableSFX`). Beep de zona roja con volumen y ritmo crecientes.
- Onboarding: `TooltipController` añadido a `1_Game` (objeto `GameStatus-Canvas/Onboarding`); pausa real (estado Paused) de 2s por tooltip.
- Hápticos con Nice Vibrations (`HapticPatterns.PlayConstant`): 80ms daño, 40ms élite; dash y pickup quedan como toques mínimos.
- Hitbox del jugador: `CircleCollider2D` de radio 0.3 (antes triángulo de ~1 unidad).
- El dash ya no se dispara fuera de Playing.
- Sprites procedurales de consumibles: el alfa estaba invertido (cuadrado con agujero); ahora son contornos de su figura.

**Upgrades de sinergia (GDD)** — assets en `Upgrades/Common/6-9` y `Upgrades/Rare/6-9`, añadidos a los pools de `UpgradeManager`.
- Comunes: Arco amplio (+30% rango), Cadena temporal, Dash veloz (-25% cooldown), Magnetismo.
- Raros: Onda de dash, Reloj voraz (+50% tiempo por baja, 15s), Fragmentación, Zona muerta (`ToxicZone` reconvertida: daña enemigos).
- Estado por partida: `UpgradeManager` (cadena, voraz, fragmentación, imán) y `PlayerCombat` (onda, zona). Daño en área: `EnemyManager.DamageEnemiesInRadius`.

**Balance**: recompensa del rebaño 0.75→1.0s; HP del élite 30→20.

**Enemigos**: colores de la paleta del GDD por tipo; proyectil magenta. Tirador en fase 2 desde los 150s (predicción de trayectoria y -25% cooldown).

**Muerte y Game Over (segunda pasada)**
- `PlayerDeathSequence` (en el Player): congela, la cámara se centra y hace zoom, el jugador se carga y estalla en pedazos (destello, anillos, partículas, SFX `playerDeathSFX`, háptico largo). `GameManager.TriggerGameOver` guarda al instante pero lanza `OnGameOver` al terminar la animación. Al volver a Playing (reiniciar/revivir) restaura cámara y jugador.
- Panel de Game Over reorganizado (Header, StatsCard, CronosLine, PrimaryButtons, SecondaryButtons) con entrada animada en cascada; un toque la completa. Los botones se animan por alfa/posición porque su Animator controla la escala.
- Cartas de upgrade: halo `RarityGlow` (sprite 9-slice `Sprites/UI/CardGlow.png`) azul/dorado según rareza, se revela al girar la carta.
- Borrados por no usarse: `GeometryRenderer`, `SkinRenderer`, `BootstrapInitializer`, `UIManager`, `ObjectPooler`, `TimeUI`.

**HUD y pantallas**: reloj grande centrado que late en alerta/peligro y destella al ganar/perder tiempo; barra por estado (dorada con Reloj voraz); línea de estado (aviso de élite, Reloj voraz); viñeta roja en ≤5s. Barra de la ventana de upgrade en rojo con <3s. Game Over: botones `+10-20 Cronos` (anuncio) y `Shop` (abre la tienda en el menú).

---

## 12. Cola de mejoras y enemigos de progresión

**Bug: enemigos destruidos al cerrar la cola de mejoras.** La Fragmentación no tenía límite: cada enemigo de 1 HP muerto por una explosión volvía a explotar. Las explosiones encoladas se congelan bajo la ventana de mejora y detonaban todas al cerrarla, vaciando la arena; esas bajas abrían a su vez más ventanas. Ahora la cadena se corta en 2 generaciones (`UpgradeManager.FRAGMENT_MAX_GENERATIONS`). Además los consumibles sólo caen de bajas directas (o del élite): las muertes en área multiplicaban los "limpiar pantalla".

**Anuncio de revivir**: reintento de carga con espera creciente, watchdog sólo hasta que el anuncio se abre, margen de 1s si el cierre llega antes que la recompensa, botones atenuados cuando no se pueden pulsar.

**10 enemigos de progresión** (`SpawnManager.progressionEnemies`, prefabs en `Prefabs/Enemys`, sprites SDF en `Sprites/Enemies`). Se desbloquean por tiempo de partida, cada uno debuta con un grupo garantizado y el aviso "NEW: …" en el HUD. La probabilidad de que un spawn sea de progresión sube de 0 (45s) a 55% (225s); el resto sigue la tabla clásica.

| Enemigo | Figura / color | HP | Desbloqueo | Mecánica |
|---|---|---|---|---|
| Splitter | Pentágono #9B5CFF | 3 | 60s | Se divide en 2 copias rápidas al morir |
| Charger | Punta de flecha #B6FF3B | 2 | 75s | Se planta, avisa y embiste en línea recta |
| Orbiter | Anillo #1FE0C4 | 2 | 90s | Orbita fuera de alcance y pica periódicamente |
| Bomber | Octágono #FFE14D | 1 | 105s | Se arma cerca y estalla (daña al jugador y a enemigos) |
| Summoner | Hexagrama #FF3DF5 | 4 | 120s | Lejos, invoca 2 esbirros cada 4.5s |
| Shielder | Escudo #3D6BFF | 3 | 135s | Escudo frontal: sólo cae por la espalda o con daño en área |
| Blinker | Reloj de arena #C9803A | 2 | 150s | Se teletransporta junto al jugador tras marcar el destino |
| Healer | Cruz #3DFF7A | 2 | 165s | Cura a los enemigos cercanos cada 3s |
| Swarm | Dardo #9AA7B8 | 1 | 180s | Grupos de 5, más rápidos que el jugador, kamikaze |
| Phantom | Media luna #C8B6FF | 2 | 210s | Alterna fase intocable/visible |

Ganchos nuevos en `EnemyBase`: `IsTargetable`, `CanHurtPlayer`, `OnHit` virtual, `Heal`, `SetTint`, `DamagePlayer`, `OwnerPool` (el pool de origen; `SpawnManager.ReleaseEnemy` ya no mira el tipo).


---

## 13. Anti-inmortalidad: topes de mejoras y Overtime

**Problema:** la dificultad tenía techo (spawn mínimo 0.45s, 55 enemigos vivos, vida fija) y las mejoras se podían repetir sin límite (alcance que cubría la pantalla, drenaje al 10%, 10 ataques/s). Pasados unos minutos el reloj quedaba clavado en 45s y la partida no terminaba nunca, farmeando cientos de Cronos.

**Topes por mejora:** `UpgradeData.maxStacks` (0 = sin límite). Al llegar al tope la mejora deja de ofrecerse. Techos absolutos en `UpgradeEffects`: alcance ≤ 6.5, cadencia ≥ 0.25s, drenaje ≥ 50%. Las mejoras que devuelven tiempo (Chronos Charge, Second Wind, Voracious Clock) no tienen tope: el Overtime ya las debilita.

**Overtime** (bloque "Overtime" del `SpawnManager`): a partir de 3:00 sube un nivel por minuto, sin techo. Drenaje, ingresos y vida crecen de forma exponencial (con escalado lineal una build al máximo seguía ganando tiempo en el nivel 6). Por nivel:

| Efecto | Por nivel |
|---|---|
| Drenaje del reloj | ×1.35 acumulativo |
| Todo el tiempo ganado (bajas, cadenas, consumibles, mejoras) | ×0.7 acumulativo |
| Vida de los enemigos que aparecen | ×1.35 acumulativo (redondeado) |
| Velocidad de los enemigos | +6% (máx. ×1.5) |
| Tiempo que quita cada golpe | +25% |
| Enemigos vivos permitidos | +2 (máx. 45; ver sección 15) |

Medido con todas las mejoras al tope e invulnerable: el reloj aguanta en el nivel 4, empieza a caer en el 6 y se vacía en el 8 (~10 min). Una partida normal, que recibe golpes, termina mucho antes.

El HUD anuncia cada nivel ("OVERTIME!", "OVERTIME 2"…) y lo deja fijo bajo el reloj. Revivir no reinicia el nivel.

---

## 14. Overtime como jefe: Chrono Warden y barreras

Cada nivel de Overtime (cada minuto desde 3:00) trae un jefe, `EnemyBoss` (engranaje carmesí, `Prefabs/Enemys/EnemyBoss.prefab`, sprite `Sprites/Enemies/EnemyGear.png`). Sólo hay un jefe a la vez: si al subir de nivel el anterior sigue vivo, el nuevo espera en cola (`PendingBossCount`, la barra del HUD muestra `BARRIER +N`) y aparece cuando termina la rotura de la barrera actual, con la vida del nivel en que sale.

- **Jefe:** 60 HP × multiplicador de vida del nivel. Persigue despacio y dispara anillos de 12 proyectiles cada 3.5s; bajo el 50% se enfurece (16 proyectiles cada 2.4s, más rápido). Cada anillo gira medio hueco respecto al anterior. Avisa parpadeando y creciendo antes de disparar. No se recicla por distancia ni al revivir; el consumible de limpiar pantalla le quita un 25% en vez de matarlo. Suelta un consumible garantizado.
- **Mientras vive:** drenaje ×1.2 extra, spawn ×1.35 más rápido, música de tensión con alarma, bordes rojos latiendo como un corazón y barra `BARRIER` con su vida en el HUD.
- **Al derrotarlo (barrera rota):** `TimeManager.RaiseMaxTime` sube el tope del reloj +5s y suma esos 5s sin la reducción del Overtime. Onda dorada, partículas, sonido y vibración; cartel `BARRIER BROKEN / MAX TIME Ns` con los bordes destellando en dorado. Si se abre una ventana de mejora, el cartel espera a que se cierre.
- **Pantalla rota:** al caer el jefe el juego se congela (`barrierCrackDuration`, 0.45s en tiempo real) y `ScreenShatter` (canvas `ScreenShatter-Canvas`, orden 100) captura la pantalla con el HUD incluido y la agrieta en telaraña desde donde cayó. Luego estalla: los pedazos salen disparados y caen, y en ese instante mueren todos los enemigos (`EnemyManager.KillAllEnemies`, cuentan como bajas sin tiempo) y desaparecen los proyectiles. Las ventanas de mejora que dispare la limpieza esperan `barrierUpgradeHold` (1.3s) con `UpgradeManager.HoldUpgrades` para no tapar el cristal ni el cartel. Sonidos: `barrierCrackSFX` (BulletCrack1) y `barrierShatterSFX` (Glass2) en el `AudioManager`.
- Ajustes en el bloque "Overtime · Jefe y barreras" del `SpawnManager` y "Overtime · Jefe" del `HUDController`. El tope del reloj ya no es constante: `TimeManager.MaxTime` (vuelve a 45 al empezar partida).

## 15. Densidad de enemigos y revivir rearmándose

**Problema:** el spawn llega hasta ~6.7 enemigos/s, así que el tope de enemigos vivos es lo único que decide cuánto se llena la pantalla, y la cámara fija hace que casi todos estén a la vista. La escena tenía 55 vivos desde el 1:30 y +10 por nivel de Overtime hasta 95: medido, 51 en pantalla al 1:47 y 94 en el Overtime 4. Ilegible en móvil.

**Cambio:** el tope crece con la partida en vez de saturarse al minuto y medio. `startActiveEnemies` 18 → `maxActiveEnemies` 35 de forma lineal hasta las 3:00; en Overtime +2 por nivel hasta `overtimeMaxActiveEnemies` 45. La dificultad del Overtime sigue en vida, velocidad, drenaje e ingresos exponenciales (sección 13) y en la mezcla de enemigos de progresión, no en la cantidad. Élites, jefes y copias del divisor siguen fuera del tope.

**Revivir:** `PlayerDeathSequence.PlayRevive` reproduce la muerte al revés. Los pedazos aparecen dispersos y aceleran girando hacia el jugador, un destello se cierra sobre él y se rearma grande y parpadeando hasta asentarse (anillos, partículas, `reviveSFX` = Boost1, vibración); luego la cámara se aleja a su encuadre. `GameManager.Revive` limpia el entorno y rellena el reloj al instante, pero la partida sigue congelada en GameOver (HUD oculto) hasta que termina la animación (~1.65s: `gatherDuration`, `reformDuration`, `zoomOutDuration`); después aplica la invulnerabilidad de gracia y vuelve a Playing.
