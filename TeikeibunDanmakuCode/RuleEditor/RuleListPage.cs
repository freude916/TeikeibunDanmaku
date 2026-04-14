using Godot;
using TeikeibunDanmaku.Core.Rules;
using TeikeibunDanmaku.RuleEditor.I18n;

namespace TeikeibunDanmaku.RuleEditor;

public sealed partial class RuleListPage : PanelContainer
{
    private Label _fileLabel = null!;
    private ItemList _ruleList = null!;
    private Label _statusLabel = null!;

    public event Action? AddRequested;
    public event Action<int>? EditRequested;
    public event Action<int>? DeleteRequested;
    public event Action<int>? MoveUpRequested;
    public event Action<int>? MoveDownRequested;
    public event Action? SaveRequested;
    public event Action? SwitchFileRequested;
    public event Action? CloseRequested;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        var margin = GetNode<MarginContainer>("Margin");
        margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        margin.SizeFlagsVertical = SizeFlags.ExpandFill;

        var root = GetNode<VBoxContainer>("Margin/Root");
        root.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.SizeFlagsVertical = SizeFlags.ExpandFill;

        _fileLabel = GetNode<Label>("%FileLabel");
        _ruleList = GetNode<ItemList>("%RuleList");
        _statusLabel = GetNode<Label>("%StatusLabel");

        _ruleList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _ruleList.SizeFlagsVertical = SizeFlags.ExpandFill;

        GetNode<Button>("%AddButton").Text = EditorLoc.T("list.add");
        GetNode<Button>("%EditButton").Text = EditorLoc.T("list.edit");
        GetNode<Button>("%DeleteButton").Text = EditorLoc.T("list.delete");
        GetNode<Button>("%MoveUpButton").Text = EditorLoc.T("list.move_up");
        GetNode<Button>("%MoveDownButton").Text = EditorLoc.T("list.move_down");
        GetNode<Button>("%SaveButton").Text = EditorLoc.T("list.save_reload");
        GetNode<Button>("%SwitchFileButton").Text = EditorLoc.T("list.switch_file");
        GetNode<Button>("%CloseButton").Text = EditorLoc.T("list.close");

        _ruleList.ItemActivated += index => EditRequested?.Invoke((int)index);
        GetNode<Button>("%AddButton").Pressed += () => AddRequested?.Invoke();
        GetNode<Button>("%EditButton").Pressed += () => EmitIndex(EditRequested);
        GetNode<Button>("%DeleteButton").Pressed += () => EmitIndex(DeleteRequested);
        GetNode<Button>("%MoveUpButton").Pressed += () => EmitIndex(MoveUpRequested);
        GetNode<Button>("%MoveDownButton").Pressed += () => EmitIndex(MoveDownRequested);
        GetNode<Button>("%SaveButton").Pressed += () => SaveRequested?.Invoke();
        GetNode<Button>("%SwitchFileButton").Pressed += () => SwitchFileRequested?.Invoke();
        GetNode<Button>("%CloseButton").Pressed += () => CloseRequested?.Invoke();
    }

    public void SetData(string? currentFilePath, IReadOnlyList<RuleDto> rules, bool isDirty, string status)
    {
        var fileName = string.IsNullOrWhiteSpace(currentFilePath)
            ? EditorLoc.T("list.file_none")
            : Path.GetFileName(currentFilePath);

        _fileLabel.Text = $"{EditorLoc.T("list.file")}: {fileName}{(isDirty ? " *" : string.Empty)}";

        _ruleList.Clear();
        for (var index = 0; index < rules.Count; index++)
        {
            var rule = rules[index];
            _ruleList.AddItem($"{index + 1}. {rule.RuleId} [{rule.Timepoint}]");
        }

        _statusLabel.Text = status;
    }

    private void EmitIndex(Action<int>? action)
    {
        if (action == null)
        {
            return;
        }

        var selected = _ruleList.GetSelectedItems();
        if (selected.Length == 0)
        {
            return;
        }

        action(selected[0]);
    }
}
