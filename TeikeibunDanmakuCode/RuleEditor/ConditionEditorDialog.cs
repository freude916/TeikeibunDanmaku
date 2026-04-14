using Godot;
using TeikeibunDanmaku.Core.Blackboard;
using TeikeibunDanmaku.Core.Condition;
using TeikeibunDanmaku.RuleEditor.Model;
using TeikeibunDanmaku.RuleEditor.Services;

namespace TeikeibunDanmaku.RuleEditor;

public sealed partial class ConditionEditorDialog : PanelContainer
{
    private Label _pathLabel = null!;
    private OptionButton _typeSelect = null!;
    private OptionButton _keySelect = null!;
    private LineEdit _valueInput = null!;
    private ItemList _childrenList = null!;
    private Label _statusLabel = null!;

    private readonly ConditionSchemaService _schemaService = new();
    private ConditionDto _current = null!;
    private string _timepointId = string.Empty;
    private IReadOnlyList<int> _path = [];
    private readonly Dictionary<string, IReadOnlyList<BoardFieldDescriptor>> _fieldsByType = new(StringComparer.Ordinal);

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
        string status)
    {
        _timepointId = timepointId;
        _path = path.ToArray();
        _current = condition.Clone();

        _pathLabel.Text = $"条件路径: {BuildPathText(_path)}";

        _typeSelect.Clear();
        _fieldsByType.Clear();

        for (var i = 0; i < types.Count; i++)
        {
            var type = types[i];
            _typeSelect.AddItem(_schemaService.GetConditionTypeDisplayName(type));
            _typeSelect.SetItemMetadata(i, type);
            _fieldsByType[type] = _schemaService.GetAllowedFieldDescriptors(_timepointId, type);
        }

        SelectType(_current.Type);
        RebuildKeyOptions(_current.Type, _current.Key);
        _valueInput.Text = GetValueInputText(_current.Type, _current.Value);
        RebuildChildren();
        RefreshVisibility();
        SetStatus(status);
    }

    public ConditionDto GetCondition()
    {
        var type = GetSelectedType();
        if (type is ConditionType.CondAnd or ConditionType.CondOr)
        {
            return new ConditionDto
            {
                Type = type,
                Conditions = _current.Conditions?.Select(c => c.Clone()).ToArray() ?? []
            };
        }

        var key = GetSelectedKey();
        return new ConditionDto
        {
            Type = type,
            Key = key,
            Value = BuildValueFromInput(type, _valueInput.Text)
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
        var type = GetSelectedType();
        var isLogic = type is ConditionType.CondAnd or ConditionType.CondOr;

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
            var typeName = _schemaService.GetConditionTypeDisplayName(child.Type);
            var keyName = _schemaService.GetFieldDisplayName(_timepointId, child.Key);
            _childrenList.AddItem($"{i + 1}. {typeName} ({(string.IsNullOrWhiteSpace(keyName) ? "-" : keyName)})");
        }
    }

    private void SelectType(string type)
    {
        for (var i = 0; i < _typeSelect.ItemCount; i++)
        {
            var metadata = _typeSelect.GetItemMetadata(i);
            if (metadata.VariantType == Variant.Type.String && string.Equals(metadata.AsString(), type, StringComparison.Ordinal))
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
        if (!_fieldsByType.TryGetValue(type, out var fields))
        {
            return;
        }

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            _keySelect.AddItem(field.DisplayName);
            _keySelect.SetItemMetadata(i, field.Name);
        }

        if (_keySelect.ItemCount == 0)
        {
            return;
        }

        var selectedIndex = 0;
        for (var i = 0; i < _keySelect.ItemCount; i++)
        {
            var metadata = _keySelect.GetItemMetadata(i);
            if (metadata.VariantType == Variant.Type.String && string.Equals(metadata.AsString(), preferredKey, StringComparison.Ordinal))
            {
                selectedIndex = i;
                break;
            }
        }

        _keySelect.Select(selectedIndex);
    }

    private string GetSelectedType()
    {
        if (_typeSelect.Selected < 0 || _typeSelect.Selected >= _typeSelect.ItemCount)
        {
            return string.Empty;
        }

        var metadata = _typeSelect.GetItemMetadata(_typeSelect.Selected);
        return metadata.VariantType == Variant.Type.String ? metadata.AsString() : string.Empty;
    }

    private string? GetSelectedKey()
    {
        if (_keySelect.Selected < 0 || _keySelect.Selected >= _keySelect.ItemCount)
        {
            return null;
        }

        var metadata = _keySelect.GetItemMetadata(_keySelect.Selected);
        return metadata.VariantType == Variant.Type.String ? metadata.AsString() : null;
    }

    private static string BuildPathText(IReadOnlyList<int> path)
    {
        return path.Count == 0 ? "root" : string.Join('.', path);
    }

    private static object? BuildValueFromInput(string type, string input)
    {
        _ = type;
        return input;
    }

    private static string GetValueInputText(string type, object? value)
    {
        _ = type;
        return value?.ToString() ?? string.Empty;
    }
}
