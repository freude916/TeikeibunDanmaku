using Godot;

namespace TeikeibunDanmaku.Frontend;

public static class DanmakuItemFactory
{
    public static DanmakuItem CreateForTestInput(string text)
    {
        return CreateDefault(text);
    }

    public static DanmakuItem CreateForAutoDanmaku(string text)
    {
        return CreateDefault(text);
    }

    public static float ResolveScrollSpeedPxPerSecond(string text)
    {
        _ = text;
        return DanmakuFrontendConfig.DefaultScrollSpeedPxPerSecond;
    }

    public static Color ResolveColor(string text)
    {
        _ = text;
        return DanmakuFrontendConfig.DefaultDanmakuColor;
    }

    private static DanmakuItem CreateDefault(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var normalizedText = text.Trim();
        return new DanmakuItem(
            normalizedText,
            ResolveColor(normalizedText),
            ResolveScrollSpeedPxPerSecond(normalizedText)
        );
    }
}
