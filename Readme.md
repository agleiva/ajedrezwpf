# Ajedrez en C# y WPF

![Demo](Demo2.gif "Demo")

## Juego completo de Ajedrez implementado en SOLO 325 líneas de código C# 8.0 y 177 líneas de XAML usando WPF corriendo sobre .NET Core 3.0

## Características:

  - Dos jugadores (Negras y Blancas)
  - Reglas básicas de movimientos de todas las piezas:
     - Peón
     - Alfil
     - Caballo
     - Torre
     - Reina
     - Rey
  - Marca en verde los casilleros válidos para mover
  - Historial de Movimientos (Panel Izquierdo)
  - Contador de piezas para cada jugador (Panel Derecho)
  - Se ajusta a la resolución de pantalla ("Responsive")
  - Menú principal con 4 botones. El cargar/guardar todavía no está implementado, pero es trivial de hacer, ya que de hecho la lógica para leer los datos de un archivo ya está funcionando y se utiliza al iniciar una nueva partida para leer los datos del archivo "Default.brd"
  - **SOLO 325 LINEAS DE C#**. 
  
### Detalles Visuales / UX:
  
  - Al seleccionar una pieza para mover, se genera una sombra que da una apariencia de relieve.
  - Los casilleros válidos para mover se marcan con un marco verde ANIMADO. En caso de que haya una pieza del oponente para capturar, se marcan en rojo, con una animación diferente. Esto en el video de demo no se nota bien porque la app que usé para grabar la pantalla tiene menos framerate, pero la animación es super fluida en el juego.
  - El botón de la barra de título que desplega el menú principal lo hace de forma animada.
  - La construcción del tablero y el posicionamiento de las piezas al iniciar la partida se hace de forma "animada", agregando las piezas una a una de forma aleatoria y con un mini-delay de 50 milisegundos. Esto da una sensación de animación al tablero.
  - Estilos visuales con temas (MahApps.Metro). Cambiando una sola linea de XAML puedo darle diferentes estilos/temas por color:
  
![Cobalt](Cobalt.png "Cobalt")

![Dark-Blue](Dark-Blue.png "Dark-Blue")

## Faltantes:

  - lógica de jaque.
  - ciclo de juego (Nuevo juego, guardar, cargar, etc).
  - Notación Algebraica.
  
  