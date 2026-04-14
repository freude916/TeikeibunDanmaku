using Godot;

namespace TeikeibunDanmaku.RuleEditor;

public sealed partial class RuleEditorEntryNode : Node
{
    private RuleEditorHost? _host;

    public override void _Ready()
    {
        _host = new RuleEditorHost();
        AddChild(_host);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.F6 })
        {
            return;
        }

        _host?.Toggle();
        GetViewport()?.SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        _host?.Dispose();
        _host = null;
    }
}
