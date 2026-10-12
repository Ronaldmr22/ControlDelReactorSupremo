using System.Collections.Generic;

namespace BattleArena.Core
{
    /// <summary>
    /// Representa qué comandos tiene presionados un jugador en este momento.
    /// </summary>
    public class EstadoComandos
    {
        // Conjunto de comandos actualmente presionados.
        // HashSet evita duplicados y permite saber si una pulsación es nueva.
        private readonly HashSet<ComandoJuego> presionados = new HashSet<ComandoJuego>();

        /// <summary>Identificador del jugador dueño de este estado.</summary>
        public string PlayerId { get; }

        // Se enciende cuando Acción se pulsa y se apaga cuando alguien la consulta.
        private bool actionPending;

        /// <summary>Crea el estado de comandos para el jugador indicado.</summary>
        /// <param name="playerId">Identificador único del jugador.</param>
        public EstadoComandos(string playerId)
        {
            PlayerId = playerId;
        }

        // Agrega el comando al conjunto de presionados. Si el comando es Acción y no estaba ya presionado, deja la acción pendiente.
        public void Press(ComandoJuego command)
        {
            bool isNewPress = presionados.Add(command);

            if (isNewPress && command == ComandoJuego.Action)
            {
                actionPending = true;
            }
        }

        // Método para cuando se suelta una tecla. Quita el comando del conjunto.
        public void Release(ComandoJuego command)
        {
            presionados.Remove(command);
        }

        /// <summary>
        /// Devuelve true una sola vez por cada pulsación nueva de Acción.
        /// Al consultarla, se "consume" el aviso.
        /// </summary>
        public bool ConsumeAction()
        {
            bool resultado = actionPending;
            actionPending = false;
            return resultado;
        }

        /// <summary>
        /// Eje horizontal: -1 (izquierda), 0 (quieto) o 1 (derecha).
        /// Si se presionan ambos lados a la vez, se cancelan y el resultado es 0.
        /// </summary>
        public int EjeHorizontal
        {
            get
            {
                int derecha = 0;
                int izquierda = 0;

                if (presionados.Contains(ComandoJuego.Right)) derecha = 1;
                if (presionados.Contains(ComandoJuego.Left)) izquierda = 1;

                return derecha - izquierda;
            }
        }

        /// <summary>
        /// Eje vertical: -1 (abajo), 0 (quieto) o 1 (arriba).
        /// Si se presionan ambos sentidos a la vez, se cancelan y el resultado es 0.
        /// </summary>
        public int EjeVertical
        {
            get
            {
                int arriba = 0;
                int abajo = 0;

                if (presionados.Contains(ComandoJuego.Up)) arriba = 1;
                if (presionados.Contains(ComandoJuego.Down)) abajo = 1;

                return arriba - abajo;
            }
        }
    }
}

