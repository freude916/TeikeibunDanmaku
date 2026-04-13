using Godot;

namespace TeikeibunDanmaku.Frontend;

public static class DanmakuFrontendConfig
{
    public const float DefaultScrollSpeedPxPerSecond = 260f;
    public const int DanmakuFontSize = 24;

    public const float TrackHeightPx = 34f;
    public const float TrackSpacingPx = 24f;
    public const float SpawnPaddingPx = 16f;
    public const float DespawnPaddingPx = 32f;

    public const float OverlayTopPaddingPx = 200f;
    public const float OverlayBottomPaddingPx = 84f;

    public const float InputPanelWidthPx = 460f;
    public const float InputPanelHeightPx = 44f;
    public const float InputPanelMarginPx = 12f;

    public static readonly Color DefaultDanmakuColor = Colors.White;
    public static readonly Color InputPanelBackground = new(0f, 0f, 0f, 0.55f);
}
