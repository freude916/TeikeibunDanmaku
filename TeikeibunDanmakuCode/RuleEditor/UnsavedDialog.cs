using Godot;
using TeikeibunDanmaku.RuleEditor.I18n;

namespace TeikeibunDanmaku.RuleEditor;

public sealed partial class UnsavedDialog : PanelContainer
{
    public event Action? SaveRequested;
    public event Action? DiscardRequested;
    public event Action? ReturnRequested;

    public override void _Ready()
    {
        GetNode<Label>("%PromptLabel").Text = EditorLoc.T("unsaved.prompt");
        GetNode<Button>("%SaveButton").Text = EditorLoc.T("unsaved.save");
        GetNode<Button>("%DiscardButton").Text = EditorLoc.T("unsaved.discard");
        GetNode<Button>("%ReturnButton").Text = EditorLoc.T("unsaved.return");

        GetNode<Button>("%SaveButton").Pressed += () => SaveRequested?.Invoke();
        GetNode<Button>("%DiscardButton").Pressed += () => DiscardRequested?.Invoke();
        GetNode<Button>("%ReturnButton").Pressed += () => ReturnRequested?.Invoke();
    }
}
