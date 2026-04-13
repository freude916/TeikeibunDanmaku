using Godot;

namespace TeikeibunDanmaku.Frontend;

public sealed class DanmakuItem
{
    public DanmakuItem(string text, Color color, float scrollSpeedPxPerSecond)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Danmaku text cannot be empty.", nameof(text));

        if (scrollSpeedPxPerSecond <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scrollSpeedPxPerSecond), "Scroll speed must be positive.");

        Text = text;
        Color = color;
        ScrollSpeedPxPerSecond = scrollSpeedPxPerSecond;
    }

    public string Text { get; }
    public Color Color { get; }
    public float ScrollSpeedPxPerSecond { get; }

    public Color GetDisplayColor()
    {
        return Color;
    }

    public float GetScrollSpeedPxPerSecond()
    {
        return ScrollSpeedPxPerSecond;
    }
}
