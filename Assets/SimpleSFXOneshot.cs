using UnityEngine;

public class SimpleSFXOneshot : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField] AudioClip audioClip;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySFX()
    {
        if(audioSource != null && audioClip != null)
            audioSource.PlayOneShot(audioClip);
    }
}
