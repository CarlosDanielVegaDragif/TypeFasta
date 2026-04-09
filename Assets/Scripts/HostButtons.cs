using UnityEngine;
using Mirror;

/// <summary>
/// Muestra ciertos botones solo al host (servidor + cliente local).
/// Usa OnStartClient en vez de Start() para garantizar que Mirror
/// ya termino de inicializar la conexion.
/// </summary>
public class HostButtons : NetworkBehaviour
{
    [SerializeField] private GameObject[] buttons;

    public override void OnStartClient()
    {
        // isServer es true en el host (que es servidor y cliente a la vez)
        bool showButtons = isServer;

        foreach (var btn in buttons)
            if (btn != null) btn.SetActive(showButtons);
    }
}