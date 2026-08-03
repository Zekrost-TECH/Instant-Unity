# Documento Técnico — Cambios Realizados y Pendientes

**Proyecto:** Instant-Unity

**Fecha:** 18 de junio de 2026

**Estado:** Implementación de código completada. Compilación exitosa. Falta conexión de componentes en el editor de Unity.

---

## 1. Resumen ejecutivo

Se implementó en código la mayoría de los elementos faltantes del documento de diseño para cumplir con las fases 2, 3 y 4 del proyecto:

- **Fase 2 (Game feel y gameplay completo):** partículas, hit flash, glow de élite, reloj pulsante, dash trail, ventana de upgrade con pausa parcial y timeout.
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
- Se añadió evento `OnKillsThresholdReached` que se dispara cada 20 kills.
- Se añadieron `ResetKillCount()` y `ResetKillsSinceLastUpgrade()`.

### 2.4 UpgradeManager.cs

**Cambios:**

- Se implementó upgrade común cada 20 kills (escucha `EnemyManager.OnKillsThresholdReached`).
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
