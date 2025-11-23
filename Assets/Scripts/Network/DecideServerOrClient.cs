using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class AutoStartServer : MonoBehaviour
{
    void Start()
    {
        // Проверяем аргументы запуска
        if (System.Environment.GetCommandLineArgs().Contains("-dedicated"))
        {
            NetworkManager.Singleton.StartServer();
            Debug.Log("Started as Dedicated Server");
        }
        else
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("Started as Client");
        }
    }
}