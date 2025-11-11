using System;
using System.Collections.Generic;

public static class EventManager
{
    public static event Action<int> onStartQuest;
    public static void StartQuest(int index)
    {
        onStartQuest?.Invoke(index);
    }

    public static event Action<Quest> onAdvanceQuest;
    public static void AdvanceQuest(Quest quest)
    {
        onAdvanceQuest?.Invoke(quest);
    }

    public static event Action<Quest> onFinishQuest;
    public static void FinishQuest(Quest quest)
    {
        onFinishQuest?.Invoke(quest);
    }

    public static event Action onMoreThanTwoCardDetected;
    public static void MoreThanTwoCardDetected()
    {
        onMoreThanTwoCardDetected?.Invoke();
    }
}