using UnityEngine;

public class RestartQuestManager : MonoBehaviour
{
    public GameObject questObject;

    void OnEnable()
    {
        if (questObject != null)
        {
            questObject.SetActive(true);
        }
    }
}
