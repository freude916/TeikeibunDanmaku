using Godot;

namespace TeikeibunDanmaku.RuleEditor;

public sealed partial class ConditionChildRow : HBoxContainer
{
    public void SetText(string text)
    {
        GetNode<Label>("%RowLabel").Text = text;
    }
}
