namespace BattleArena.Core
{
    /// <summary>
    /// Comandos de juego que un jugador puede ejecutar.
    /// Corresponden a los cinco controles físicos exigidos por el enunciado (US-11 y US-12).
    /// Es independiente de la fuente de entrada: teclado, mando, hardware
    /// producen los mismos comandos.
    /// </summary>
    public enum ComandoJuego
    {
        /// <summary>Mover hacia arriba.</summary>
        Up,

        /// <summary>Mover hacia abajo.</summary>
        Down,

        /// <summary>Mover hacia la izquierda.</summary>
        Left,

        /// <summary>Mover hacia la derecha.</summary>
        Right,

        /// <summary>Ejecutar la acción del juego (paralizar).</summary>
        Action
    }
}
