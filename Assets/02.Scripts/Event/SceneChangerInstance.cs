using UnityEngine;

public class SceneChangerInstance : MonoBehaviour
{
    public void SceneChangeInstance(string sceneName)
    {
        SceneChanger sceneChanger = FindFirstObjectByType<SceneChanger>();
        if (sceneChanger != null)
        {
            sceneChanger.StartLoadScene(sceneName);
        }
    }
}
