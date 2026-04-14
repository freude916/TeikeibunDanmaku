using Godot;
using TeikeibunDanmaku.Core.Condition;
using TeikeibunDanmaku.RuleEditor.I18n;
using TeikeibunDanmaku.RuleEditor.Model;

namespace TeikeibunDanmaku.RuleEditor;

public sealed partial class ConditionEditorDialog : PanelContainer
{
    private Label _pathLabel = null!;
    private OptionButton _typeSelect = null!;
    private OptionButton _keySelect = null!;
    private LineEdit _valueInput = null!;
    private ItemList _childrenList = null!;
    private Label _statusLabel = null!;

    private ConditionDto _current = null!;
    private string _timepointId = string.Empty;
    private IReadOnlyList<int> _path = [];
    private IReadOnlyList<string> _types = [];
    private readonly Dictionary<string, IReadOnlyList<string>> _keysByType = new(StringComparer.Ordinal);

    public event Action<IReadOnlyList<int>, ConditionDto>? DoneRequested;
    public event Action? CancelRequested;
    public event Action<IReadOnlyList<int>, int>? EditChildRequested;
    public event Action<IReadOnlyList<int>> AddChildRequested;
    public event Action<IReadOnlyList<int>, int>? RemoveChildRequested;

    public override void _Ready()
    {
        _pathLabel = GetNode<Label>("%PathLabel");
        _typeSelect = GetNode<OptionButton>("%TypeSelect");
        _keySelect = GetNode<OptionButton>("%KeySelect");
        _valueInput = GetNode<LineEdit>("%ValueInput");
        _childrenList = GetNode<ItemList>("%ChildrenList");
        _statusLabel = GetNode<Label>("%StatusLabel");

        GetNode<Label>("%TypeLabel").Text = EditorLoc.T("condition.type");
        GetNode<Label>("%KeyLabel").Text = EditorLoc.T("condition.key");
        GetNode<Label>("%ValueLabel").Text = EditorLoc.T("condition.value");
        GetNode<Label>("%ChildrenLabel").Text = EditorLoc.T("condition.children");
        GetNode<Button>("%AddChildButton").Text = EditorLoc.T("condition.add_child");
        GetNode<Button>("%EditChildButton").Text = EditorLoc.T("condition.edit_child");
        GetNode<Button>("%RemoveChildButton").Text = EditorLoc.T("condition.remove_child");
        GetNode<Button>("%DoneButton").Text = EditorLoc.T("common.done");
        GetNode<Button>("%CancelButton").Text = EditorLoc.T("common.cancel");

        _typeSelect.ItemSelected += _ => RefreshVisibility();
        _childrenList.ItemActivated += index => EditChildRequested?.Invoke(_path, (int)index);
        GetNode<Button>("%AddChildButton").Pressed += () => AddChildRequested?.Invoke(_path);
        GetNode<Button>("%EditChildButton").Pressed += EmitEditChild;
        GetNode<Button>("%RemoveChildButton").Pressed += EmitRemoveChild;
        GetNode<Button>("%DoneButton").Pressed += EmitDone;
        GetNode<Button>("%CancelButton").Pressed += () => CancelRequested?.Invoke();
    }

    public void Initialize(
        string timepointId,
        IReadOnlyList<int> path,
        ConditionDto condition,
        IReadOnlyList<string> types,
        Func<string, IReadOnlyList<string>> keyResolver,
        string status)
    {
        _timepointId = timepointId;
        _path = path.ToArray();
        _current = condition.Clone();
        _types = types;

        _pathLabel.Text = $"{EditorLoc.T("condition.path")}: {BuildPathText(_path)}";

        _typeSelect.Clear();
        _keysByType.Clear();
        foreach (var type in _types)
        {
            _typeSelect.AddItem(type);
            _keysByType[type] = keyResolver(type);
        }

        SelectType(_current.Type);
        RebuildKeyOptions(_current.Type, _current.Key);
        _valueInput.Text = _current.Value?.ToString() ?? string.Empty;
        RebuildChildren();
        RefreshVisibility();
        SetStatus(status);
    }

    public ConditionDto GetCondition()
    {
        var type = _typeSelect.GetItemText(_typeSelect.Selected);
        if (type is ConditionType.And or ConditionType.Or)
        {
            return new ConditionDto
            {
                Type = type,
                Conditions = _current.Conditions?.Select(c => c.Clone()).ToArray() ?? []
            };
        }

        var key = _keySelect.ItemCount == 0 ? null : _keySelect.GetItemText(_keySelect.Selected);
        return new ConditionDto
        {
            Type = type,
            Key = key,
            Value = _valueInput.Text
        };
    }

    public void ReplaceChild(int index, ConditionDto child)
    {
        var children = _current.Conditions?.Select(c => c.Clone()).ToArray() ?? [];
        if (index < 0 || index >= children.Length)
        {
            return;
        }

        children[index] = child.Clone();
        _current = new ConditionDto
        {
            Type = _current.Type,
            Conditions = children
        };

        RebuildChildren();
    }

    public void AddChild(ConditionDto child)
    {
        var children = (_current.Conditions?.Select(c => c.Clone()).ToList() ?? []);
        children.Add(child.Clone());

        _current = new ConditionDto
        {
            Type = _current.Type,
            Conditions = children
        };

        RebuildChildren();
    }

    public void RemoveChild(int index)
    {
        var children = (_current.Conditions?.Select(c => c.Clone()).ToList() ?? []);
        if (index < 0 || index >= children.Count)
        {
            return;
        }

        children.RemoveAt(index);
        _current = new ConditionDto
        {
            Type = _current.Type,
            Conditions = children
        };

        RebuildChildren();
    }

    public void SetStatus(string status)
    {
        _statusLabel.Text = status;
    }

    private void EmitDone()
    {
        DoneRequested?.Invoke(_path, GetCondition());
    }

    private void EmitEditChild()
    {
        var selected = _childrenList.GetSelectedItems();
        if (selected.Length == 0)
        {
            return;
        }

        EditChildRequested?.Invoke(_path, selected[0]);
    }

    private void EmitRemoveChild()
    {
        var selected = _childrenList.GetSelectedItems();
        if (selected.Length == 0)
        {
            return;
        }

        RemoveChildRequested?.Invoke(_path, selected[0]);
    }

    private void RefreshVisibility()
    {
        var type = _typeSelect.GetItemText(_typeSelect.Selected);
        var isLogic = type is ConditionType.And or ConditionType.Or;

        _keySelect.Visible = !isLogic;
        _valueInput.Visible = !isLogic;
        _childrenList.Visible = isLogic;
        GetNode<Label>("%KeyLabel").Visible = !isLogic;
        GetNode<Label>("%ValueLabel").Visible = !isLogic;
        GetNode<Label>("%ChildrenLabel").Visible = isLogic;
        GetNode<HBoxContainer>("%ChildButtonRow").Visible = isLogic;

        RebuildKeyOptions(type, _current.Key);
    }

    private void RebuildChildren()
    {
        _childrenList.Clear();
        var children = _current.Conditions?.ToArray() ?? [];
        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            _childrenList.AddItem($"{i + 1}. {child.Type} ({child.Key ?? "-"})");
        }
    }

    private void SelectType(string type)
    {
        for (var i = 0; i < _typeSelect.ItemCount; i++)
        {
            if (string.Equals(_typeSelect.GetItemText(i), type, StringComparison.Ordinal))
            {
                _typeSelect.Select(i);
                return;
            }
        }

        _typeSelect.Select(0);
    }

    private void RebuildKeyOptions(string type, string? preferredKey)
    {
        _keySelect.Clear();
        if (!_keysByType.TryGetValue(type, out var keys))
        {
            return;
        }

        for (var i = 0; i < keys.Count; i++)
        {
            _keySelect.AddItem(keys[i]);
        }

        if (_keySelect.ItemCount == 0)
        {
            return;
        }

        var selectedIndex = 0;
        for (var i = 0; i < _keySelect.ItemCount; i++)
        {
            if (string.Equals(_keySelect.GetItemText(i), preferredKey, StringComparison.Ordinal))
            {
                selectedIndex = i;
                break;
            }
        }

        _keySelect.Select(selectedIndex);
    }

    private static string BuildPathText(IReadOnlyList<int> path)
    {
        return path.Count == 0 ? "root" : string.Join('.', path);
    }
}
