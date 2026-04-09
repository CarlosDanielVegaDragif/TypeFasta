using Steamworks;
using UnityEngine;

public class GetSteamRunningCheck : MonoBehaviour
{
    void Start()
    {
        if (SteamManager.Initialized)
            Debug.Log("Steam OK: " + SteamFriends.GetPersonaName());
        else
            Debug.LogError("Steam no inicializado");
    }
}
