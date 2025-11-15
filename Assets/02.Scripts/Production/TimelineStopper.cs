using UnityEngine;
using UnityEngine.Playables;

public class TimelineStopper : MonoBehaviour
{
    public PlayableDirector director;
    public double stopTime = 5.0; // ∏ÿ√‚ Ω√¡° (√ )

    private bool isWatching = false;

    void Start()
    {
        if (director == null)
        {
            director = GetComponent<PlayableDirector>();
        }
    }

    public void PlayTimeline()
    {
        director.Play();
        isWatching = true;
    }

    void Update()
    {
        if (isWatching && director.time >= stopTime)
        {
            director.Pause();
            isWatching = false;
            Debug.Log($"Timeline paused at {stopTime} seconds");
        }
    }
}
