using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkManager
{
    [Header("TypeFasta")]
    [SerializeField] private string gameSceneName = "InGame";
 
    /// <summary>
    /// Llamar desde el boton Start Game del menu.
    /// Cambia la escena para todos los clientes conectados.
    /// </summary>
    public void StartGameScene()
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("[NetworkManager] Solo el servidor puede cambiar de escena.");
            return;
        }
 
        ServerChangeScene(gameSceneName);
    }
 
    /// <summary>
    /// Se llama en el servidor cuando TODOS los clientes terminaron de cargar la escena.
    /// Es el momento correcto para iniciar la partida.
    /// </summary>
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
 
        if (sceneName == gameSceneName)
        {
            // Esperar un frame para que NetworkGameManager.Instance este disponible
            StartCoroutine(StartGameNextFrame());
        }
    }
 
    private System.Collections.IEnumerator StartGameNextFrame()
    {
        yield return null;
 
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
            Debug.Log("[NetworkManager] Partida iniciada en escena: " + gameSceneName);
        }
        else
        {
            Debug.LogError("[NetworkManager] GameManager.Instance es null. " +
                           "Asegurate de que este en la escena InGame.");
        }
    }
}
