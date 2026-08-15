# Notas de Integración — Instant

## Managers nuevos a conectar en la escena de juego

Añade los siguientes componentes a objetos en la escena `1_Game` (o deja que `BootstrapInitializer` los cree automáticamente):

- `GameManager`
- `TimeManager`
- `SpawnManager`
- `EnemyManager`
- `UpgradeManager` (asignar `commonUpgrades` y `rareUpgrades`)
- `AudioManager` (asignar `musicSource`, `sfxSource`, y los clips de audio)
- `SaveManager` no debe añadirse al objeto `Managers`: `GameManager` y el menú lo crean de forma persistente.
- `SkinManager` se crea desde el menú y permanece persistente.
- `AdsManager` se crea al necesitar el revive; en Editor usa stub y en dispositivo usa AdMob.
- `HapticManager`
- `ParticleManager` (asignar prefabs de partículas de muerte, tiempo ganado y dash trail)
- `DamageNumbersManager` (asignar `enemyDamagePrefab`, `playerDamagePrefab`, `worldPopupParent` y `timePopupParent` al `GameStatus-Canvas`, y `timePopupAnchor` al `Time-Bar-Background`)
- `BootstrapInitializer` (opcional, para crear managers automáticamente)

## UI a conectar en la escena de juego

- `HUDController`: ya está asignado a `Managers` con barra de tiempo, Cronos de run, kills y HUD raíz.
- `GameOverController`: ya tiene tiempo, kills, élites, tiempo ganado, golpes, pago y récords asignados.
- `UpgradeUIManager`: ya tiene título y barra de timeout asignados; el panel se cierra automáticamente tras 8 segundos.
- `TooltipController`: asignar `tooltipPanel`, `tooltipText`, `timeTextTarget`, `dashButtonTarget`.
- `JoystickController`: asignar fondo y handle del joystick.
- `DashButtonController`: asignar `cooldownRing`, `buttonImage`.

## Player

- Añadir `SkinRenderer` al jugador.
- Asegurar que `PlayerInput` y `PlayerMovement` estén en el mismo objeto.
- El `JoystickController` y `DashButtonController` se encuentran automáticamente.

## Enemigos

- `EnemyVisualFeedback` ya está añadido a cada prefab de enemigo.
- Configurar `baseColor` y `isElite` en `EnemyBase`.
- Los enemigos élite (`EnemyElite`) activan el glow automáticamente.

## Main Menu

- En `MainMenuUI`, asignar los nuevos toggles: `musicToggle`, `sfxToggle`, `vibrationToggle`.
- Los botones de skins usan `SkinManager` automáticamente.
- El botón `Cronos` abre `PermanentProgressionUI` con mejoras de tiempo inicial, rango y dash.

## Audio

- `enemyDeathSFX` usa `Assets/Feel/NiceVibrations/HapticSamples/Weapons/ScifiGunshot1.wav`.
- También están asignados temporalmente `clockBeepSFX`, `timeGainSFX`, `upgradeMissedSFX`, música, tensión y dash desde NiceVibrations.
- `AudioManager` ya aplica fade de música al 30% durante ventanas de upgrade.

## Prefabs necesarios

Prefabs ya creados y asignados:
- `deathParticlePrefab`: `Assets/_Custom/Prefabs/VFX/EnemyDeathParticle.prefab`.
- `timeGainParticlePrefab`: `Assets/_Custom/Prefabs/VFX/TimeGainParticle.prefab`.
- `dashTrailPrefab`: `Assets/_Custom/Prefabs/VFX/DashTrail.prefab`.
- `projectilePrefab`: `Assets/_Custom/Prefabs/Combat/EnemyProjectile.prefab`.
- `DamageNumbersManager.enemyDamagePrefab`: `Assets/_Custom/Prefabs/Feedback/DamageNumbers/PlayerAttackDamageGUI.prefab`.
- `DamageNumbersManager.playerDamagePrefab`: `Assets/_Custom/Prefabs/Feedback/DamageNumbers/PlayerHitDamageGUI.prefab`.
- `DamageNumbersManager.timeChangePrefab`: `Assets/_Custom/Prefabs/Feedback/DamageNumbers/TimeChangeRed.prefab`.
