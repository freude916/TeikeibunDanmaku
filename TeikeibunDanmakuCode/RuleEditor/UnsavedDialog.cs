using Godot;

namespace TeikeibunDanmaku.RuleEditor;

public sealed partial class UnsavedDialog : PanelContainer
{
    public event Action? SaveRequested;
    public event Action? DiscardRequested;
    public event Action? ReturnRequested;

    public override void _Ready()
    {
        GetNode<Button>("%SaveButton").Pressed += () => SaveRequested?.Invoke();
        GetNode<Button>("%DiscardButton").Pressed += () => DiscardRequested?.Invoke();
        GetNode<Button>("%ReturnButton").Pressed += () => ReturnRequested?.Invoke();
    }
}
