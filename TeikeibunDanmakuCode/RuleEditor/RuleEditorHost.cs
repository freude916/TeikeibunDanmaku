using Godot;

namespace TeikeibunDanmaku.RuleEditor;

public sealed partial class RuleEditorHost : Node, IDisposable
{
    private const string RootScenePath = "res://TeikeibunDanmaku/scenes/RuleEditorRoot.tscn";

    private RuleEditorRootView? _rootView;

    public override void _Ready()
    {
        var scene = GD.Load<PackedScene>(RootScenePath)
                    ?? throw new InvalidOperationException($"Missing rule editor scene at '{RootScenePath}'.");

        _rootView = scene.Instantiate<RuleEditorRootView>();
        AddChild(_rootView);
    }

    public void Toggle()
    {
        _rootView?.Toggle();
    }

    public void Dispose()
    {
        if (_rootView == null)
        {
            return;
        }

        if (GodotObject.IsInstanceValid(_rootView))
        {
            _rootView.QueueFree();
        }

        _rootView = null;
    }
}
