using UnityEngine;
using UnityEngine.InputSystem;
using BattleArena.Core;

namespace BattleArena.Core
{
    public class FuenteEntradaTeclado : MonoBehaviour
    {
        public EstadoComandos Estado { get; private set; } // Propiedad que representa el estado del comando

        private void Awake()
        {
            Estado = new EstadoComandos("jugador-1");        // Crea la "pizarra"
        }

        private void Update()                              // Se llama en Unity en cada cuadro
        {
            Keyboard teclado = Keyboard.current;           // Es el teclado conectado
            if (teclado == null) return;                   // Si no hay teclado devuelve nada

            Sync(teclado.upArrowKey.isPressed, ComandoJuego.Up);
            Sync(teclado.downArrowKey.isPressed, ComandoJuego.Down);
            Sync(teclado.leftArrowKey.isPressed, ComandoJuego.Left);
            Sync(teclado.rightArrowKey.isPressed, ComandoJuego.Right);
            Sync(teclado.spaceKey.isPressed, ComandoJuego.Action);
        }

        private void Sync(bool presionada, ComandoJuego comando)
        {
            if (presionada)
            {
                Estado.Press(comando);
            }
            else
            {
                Estado.Release(comando);
            }
        }
    }
}