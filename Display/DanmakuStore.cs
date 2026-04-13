namespace TeikeibunDanmaku.Frontend;

public static class DanmakuStore
{
    public static event Action<string>? DanmakuRequested;

    public static void Publish(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        DanmakuRequested?.Invoke(text.Trim());
    }
}
