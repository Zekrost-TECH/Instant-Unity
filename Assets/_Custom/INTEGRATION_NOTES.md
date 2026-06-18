# Notas de Integración — Instant

## Managers nuevos a conectar en la escena de juego

Añade los siguientes componentes a objetos en la escena `1_Game` (o deja que `BootstrapInitializer` los cree automáticamente):

- `GameManager`
- `TimeManager`
- `SpawnManager`
- `EnemyManager`
- `UpgradeManager` (asignar `commonUpgrades` y `rareUpgrades`)
- `AudioManager` (asignar `musicSource`, `sfxSource`, y los clips de audio)
- `SaveManager` (usar `DontDestroyOnLoad`)
- `SkinManager`
- `AdsManager` (stub, listo para integrar LevelPlay)
- `HapticManager`
- `ParticleManager` (asignar prefabs de partículas de muerte, tiempo ganado y dash trail)
- `BootstrapInitializer` (opcional, para crear managers automáticamente)

## UI a conectar en la escena de juego

- `HUDController`: asignar `timeText`, `timeBar`, `killCountText`, `runCronosText`, `hudRoot`.
- `GameOverController`: asignar panel, textos de stats y botones (Watch Ad, Restart, Shop).
- `UpgradeUIManager`: asignar `upgradeCanvasPanel`, `cardsContainer`, `cardPrefab`, `overlayImage`, `progressBar`, `titleText`.
- `TooltipController`: asignar `tooltipPanel`, `tooltipText`, `timeTextTarget`, `dashButtonTarget`.
- `JoystickController`: asignar fondo y handle del joystick.
- `DashButtonController`: asignar `cooldownRing`, `buttonImage`.

## Player

- Añadir `SkinRenderer` al jugador.
- Asegurar que `PlayerInput` y `PlayerMovement` estén en el mismo objeto.
- El `JoystickController` y `DashButtonController` se encuentran automáticamente.

## Enemigos

- Añadir `EnemyVisualFeedback` a cada prefab de enemigo.
- Configurar `baseColor` y `isElite` en `EnemyBase`.
- Los enemigos élite (`EnemyElite`) activan el glow automáticamente.

## Main Menu

- En `MainMenuUI`, asignar los nuevos toggles: `musicToggle`, `sfxToggle`, `vibrationToggle`.
- Los botones de skins usan `SkinManager` automáticamente.

## Audio

- Añadir los clips: `clockBeepSFX`, `timeGainSFX`, `upgradeMissedSFX` al `AudioManager`.
- `AudioManager` ya aplica fade de música al 30% durante ventanas de upgrade.

## Prefabs necesarios

Crear prefabs simples para:
- `deathParticlePrefab`: objeto con `SpriteRenderer` y `Rigidbody2D`.
- `timeGainParticlePrefab`: objeto con `SpriteRenderer` y `Rigidbody2D`.
- `dashTrailPrefab`: objeto con `SpriteRenderer` escalado como rectángulo.

Asignarlos en `ParticleManager`.
