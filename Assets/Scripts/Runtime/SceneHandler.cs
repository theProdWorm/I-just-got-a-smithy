using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runtime
{
    public class SceneHandler : MonoBehaviour
    {
        public static void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
        public static void LoadScene(int sceneIndex) => SceneManager.LoadScene(sceneIndex);
        
        public static void Quit() => Application.Quit();
    }
}
