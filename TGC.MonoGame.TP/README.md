# TankWars - Grupo "Tanquesitos" <sub>(con "S" por voto popular)</sub>

Trabajo Práctico de Técnicas de Gráficos por Computadora (UTN.BA)  
Proyecto basado en la idea **TankWars - World of Tanks / Tanques**

---

## Ítems comunes a todas las ideas

- [ ] Escenario 3D con buena calidad gráfica (árboles, rocas, pasto, etc.)
- [ ] Texturas adecuadas y proporcionadas
- [ ] HUD: scoreboards, mapas, indicadores de energía
- [ ] Uso intensivo de shaders (HLSL Shader Model 3.0)
- [ ] Performance mínima de 60 FPS en hardware promedio
- [ ] Estructura lógica: presentación, menú principal, desarrollo, final
- [ ] Jugabilidad clara con condición de victoria
- [ ] Modo God para presentación
- [ ] Ventana adaptable a distintas resoluciones (responsive)
- [ ] Fullscreen en entrega final

---

## 1ra Entrega (General)

- [x] Fork del repositorio base `tgc-utn/tgc-monogame-tp`
- [x] Renombrar el proyecto siguiendo el formato `<Año>-<Cuatrimestre>-<Curso>-<NombreDelGrupo>`
- [ ] El escenario está armado:
	- [ ] sin animaciones
	- [ ] con los modelos seleccionados
	- [ ] ubicados donde corresponde
	- [ ] cuenta con un total de entre 300 a 400 modelos mínimo
- [ ] Confirmar que el proyecto compila y corre
- [ ] No usamos el BasicEffect que viene cargado por defecto e Model
---

## 2da Entrega (TankWars)

- [ ] Escenario completo con árboles, rocas, postes y obstáculos
- [ ] Controlar un tanque en tercera persona
- [ ] Otros tanques atacan al jugador
- [ ] Disparo de proyectiles con colisiones en entorno y tanques
- [ ] Terreno generado con heightmap
- [ ] Movimientos del tanque:
  - [ ] Acelerar
  - [ ] Frenar
  - [ ] Reversa
  - [ ] Doblar
- [ ] Rotación de torreta sobre eje Y
- [ ] Movimiento de cañón sobre eje X
- [ ] Colisiones precisas contra objetos y tanques
- [ ] Respuesta de colisión según velocidad y ángulo
- [ ] Elementos destruidos por colisiones o impactos

---

## 3ra Entrega (General)

Tendrá como objetivo abarcar toda la funcionalidad esencial del juego, optimización,  
jugabilidad, HUD, menú con fondo 3D (una sola ventana), música y sonidos.

- [ ] Integrar todas las funcionalidades de la 2da entrega con mejoras de performance
- [ ] Documentar jugabilidad: controles, objetivos y condición de victoria
- [ ] Incluir menú principal con opciones configurables
- [ ] Implementar estructura lógica completa: presentación, menú, desarrollo, final
- [ ] Asegurar performance mínima de 60 FPS en hardware promedio
- [ ] Subir documentación y código actualizado al repositorio
- [ ] 

---

## 4ta Entrega (TankWars)

- [ ] IA en tanques enemigos (perseguir y disparar al jugador)
- [ ] Iluminación (mínimo Blinn-Phong) en vehículos, entorno y terreno
- [ ] Sombras con Shadow Map
- [ ] Animaciones de ruedas/orugas coherentes con movimiento
- [ ] Deformación de tanques al recibir impactos (modificación de vértices)

---

## ⭐ Funcionalidades opcionales

- [ ] Sistema de partículas (humo, fuego, chispas)
- [ ] Delineado de silueta para tanques ocultos detrás de objetos
- [ ] Efecto de Bloom en disparos y humo/fuego
- [ ] Modo versus en pantalla dividida (2 jugadores, sin IA enemiga)

---

## 📌 Notas

- Proyecto: `2026-1C-3061-Tanquesitos`  
- Repositorio base: [tgc-utn/tgc-monogame-tp](https://github.com/tgc-utn/tgc-monogame-tp)
- Enunciado: [Google Doc](https://docs.google.com/document/d/1R9nnQYHihX_bXz55AWbHuvmFl8olRc1Jlz7njMbOlYg/edit?tab=t.0#heading=h.tlqk8ny52ftb)
