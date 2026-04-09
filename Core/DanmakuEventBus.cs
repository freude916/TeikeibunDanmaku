namespace TeikeibunDanmaku.Core;

public static class DanmakuEventBus
{
    public static event Action<string>? DanmakuRequested;

    public static void Publish(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        DanmakuRequested?.Invoke(text.Trim());
    }
}
