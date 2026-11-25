# World of Bugs - TGC MonoGame TP

[![.NET](https://github.com/IvanScquizzato/2026-2C-3561-WorldOfBugs/actions/workflows/dotnet.yml/badge.svg)](https://github.com/IvanScquizzato/2026-2C-3561-WorldOfBugs/actions/workflows/dotnet.yml)
[![GitHub license](https://img.shields.io/github/license/IvanScquizzato/2026-2C-3561-WorldOfBugs.svg)](LICENSE)

[#BuiltWithMonoGame](https://monogame.net/) and [.NET Core](https://dotnet.microsoft.com)

## Descripción

Trabajo práctico de la asignatura electiva [Técnicas de Gráficos por Computadora](https://tgc-utn.github.io/) (TGC), 2do cuatrimestre 2026, curso K3561. Ingeniería en Sistemas de Información, Universidad Tecnológica Nacional, Facultad Regional Buenos Aires (UTN-FRBA).

**World of Bugs** es un juego de tanques en tercera persona. Manejás un tanque por un escenario abierto con ruinas, autos abandonados, rocas, árboles y faroles, y tenés que destruir a todos los tanques enemigos antes de que se termine el tiempo.

### Objetivo

- **Victoria:** eliminar a todos los tanques enemigos.
- **Derrota:** perder toda la vida o quedarse sin tiempo.

### Controles

| Tecla | Acción |
| ----- | ------ |
| `W` / `S` | Avanzar / retroceder |
| `A` / `D` | Girar a la izquierda / derecha |
| Mouse | Apuntar la torreta |
| `X` | Disparar |
| `ESC` | Pausa |
| `C` | Cambiar a cámara libre (Solo en modo god) |

### HUD

- Barra roja: vida.
- Barra verde: recarga del disparo.
- Minimapa: posición de los enemigos.
- Temporizador: tiempo restante.

### Características técnicas

- Terreno generado a partir de un heightmap.
- Iluminación PBR con mapas de normales.
- Sombras: shadow mapping direccional y shadow maps omnidireccionales para las luces puntuales (faroles).
- Bloom en los faroles.
- Ciclo día/noche con skybox atmosférico y environment probe.
- Silueta (outline) del tanque cuando queda tapado por otros objetos.
- Física con [BepuPhysics](https://github.com/bepu/bepuphysics2): colisiones, raycasts para la suspensión del tanque y objetos destructibles.
- Sonido: música, disparos, explosiones, motor y choques.
- Menús de inicio, ayuda, pausa, victoria y derrota.
- Deformación de tanques al recibir disparos.

## Configuración del entorno de desarrollo

Los pasos a seguir según su sistema operativo se pueden leer en el siguiente documento [install.md](https://github.com/tgc-utn/tgc-monogame-samples/blob/master/docs/install/install.md).

Afuera del mundo Windows, vas a necesitar la ayuda de [Wine](https://www.winehq.org) para los shaders, por lo menos por [ahora](https://github.com/MonoGame/MonoGame/issues/2167).

Los recursos usados se almacenan utilizando [Git LFS](https://git-lfs.github.com), con lo cual antes de clonar el repositorio les conviene tenerlo instalado así es automático el pull o si ya lo tienen pueden hacer `git lfs pull`.

Para correr el juego:

```bash
dotnet run --project TGC.MonoGame.TP
```

## Integrantes

| Apellido, Nombre |
| ---------------- |
| Scquizzato, Iván |
| Vaserman, Ian |
| Miranda, Martín |
| Vidaurreta Roth, Franco |
| Petasne, Nicolás |

## Capturas

![Recorrido de día con los tanques enemigos a la vista](docs/screenshots/screenshot1.jpg)

![Monumento y farol en el centro del mapa](docs/screenshots/screenshot2.jpg)

![Casas en ruinas](docs/screenshots/screenshot3.jpg)

![Farol encendido y sombras del tanque](docs/screenshots/screenshot4.jpg)

![Atardecer: los enemigos tapados por la casa se ven con contorno](docs/screenshots/screenshot5.jpg)

![De noche, iluminado por un farol](docs/screenshots/screenshot6.jpg)

![Bosque: los enemigos ocultos se marcan con su silueta](docs/screenshots/screenshot7.jpg)

## Game Play

[![Gameplay](docs/screenshots/screenshot1.jpg)](https://www.youtube.com/watch?v=REEMPLAZAR)

## Sonidos

Todos los sonidos son de [OpenGameArt](https://opengameart.org):

- Música: [Determined Pursuit (epic orchestra loop)](https://opengameart.org/content/determined-pursuit-epic-orchestra-loop), de Emma_MA (CC0).
- Disparo: [Cannon fire](https://opengameart.org/content/cannon-fire), de Thimras (CC0).
- Explosión: [Mechanical Explosion](https://opengameart.org/content/mechanical-explosion), de Spring Spring (CC0).
- Motor: [Engine-loop heavy vehicle/tank](https://opengameart.org/content/engine-loop-heavy-vehicletank), de Nayckron ([CC-BY 3.0](https://creativecommons.org/licenses/by/3.0/)).
- Choque y árbol: [100 CC0 metal and wood SFX](https://opengameart.org/content/100-cc0-metal-and-wood-sfx), de rubberduck (CC0).
- Click del menú: [Click](https://opengameart.org/content/click), de qubodup (CC0).
