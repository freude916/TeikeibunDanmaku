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
    private Label _valueLabel = null!;
    private LineEdit _valueInput = null!;
    private HBoxContainer _itemRow = null!;
    private LineEdit _itemInput = null!;
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
        _valueLabel = GetNode<Label>("%ValueLabel");
        _valueInput = GetNode<LineEdit>("%ValueInput");
        _itemRow = GetNode<HBoxContainer>("%ItemRow");
        _itemInput = GetNode<LineEdit>("%ItemInput");
        _childrenList = GetNode<ItemList>("%ChildrenList");
        _statusLabel = GetNode<Label>("%StatusLabel");

        _typeSelect.ItemSelected += _ => RefreshVisibility();
        _keySelect.ItemSelected += _ => RefreshVisibility();
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
        _valueInput.Text = GetValueInputText(_current.Type, _current.Key, _current.Value);
        _itemInput.Text = GetItemInputText(_current.Type, _current.Key, _current.Value);
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
            Value = BuildValueFromInput(type, key, _valueInput.Text, _itemInput.Text)
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
        var selectedKey = GetSelectedKey();
        var isLogic = type is ConditionType.CondAnd or ConditionType.CondOr;
        var usesListCount = UsesListCountValue(type, GetSelectedFieldDescriptor(type, selectedKey));

        _keySelect.Visible = !isLogic;
        _valueInput.Visible = !isLogic;
        _itemRow.Visible = !isLogic && usesListCount;
        _childrenList.Visible = isLogic;
        GetNode<Label>("%KeyLabel").Visible = !isLogic;
        _valueLabel.Visible = !isLogic;
        GetNode<Label>("%ChildrenLabel").Visible = isLogic;
        GetNode<HBoxContainer>("%ChildButtonRow").Visible = isLogic;

        _valueLabel.Text = usesListCount ? "数量" : "值";
        RebuildKeyOptions(type, selectedKey ?? _current.Key);
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

        var selectedKey = GetSelectedKey();
        _valueInput.Text = GetValueInputText(type, selectedKey, _current.Value);
        _itemInput.Text = GetItemInputText(type, selectedKey, _current.Value);
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

    private object? BuildValueFromInput(string type, string? key, string valueInput, string itemInput)
    {
        var descriptor = GetSelectedFieldDescriptor(type, key);
        if (UsesListCountValue(type, descriptor))
        {
            var count = ParseCount(valueInput);
            return new Dictionary<string, object?>
            {
                ["item"] = itemInput,
                ["count"] = count
            };
        }

        return valueInput;
    }

    private static string GetValueInputText(string type, string? key, object? value)
    {
        if (UsesListCountValue(type, key, value) && TryExtractListCountValue(value, out _, out var countText))
            return countText;

        return value?.ToString() ?? string.Empty;
    }

    private static string GetItemInputText(string type, string? key, object? value)
    {
        if (UsesListCountValue(type, key, value) && TryExtractListCountValue(value, out var item, out _))
            return item;

        return string.Empty;
    }

    private BoardFieldDescriptor? GetSelectedFieldDescriptor(string conditionType, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        if (!_fieldsByType.TryGetValue(conditionType, out var fields))
            return null;

        return fields.FirstOrDefault(field => string.Equals(field.Name, key, StringComparison.Ordinal));
    }

    private static bool UsesListCountValue(string type, BoardFieldDescriptor? descriptor)
    {
        return (type is ConditionType.Eq or ConditionType.ValueLt or ConditionType.ValueGt) &&
               descriptor != null &&
               TeikeibunDanmaku.Utils.TypeUtil.IsStringEnumerableType(descriptor.ValueType);
    }

    private static bool UsesListCountValue(string type, string? key, object? value)
    {
        if (type is not (ConditionType.Eq or ConditionType.ValueLt or ConditionType.ValueGt))
            return false;

        if (value is null)
            return false;

        if (TryExtractListCountValue(value, out _, out _))
            return true;

        return !string.IsNullOrWhiteSpace(key) && value is Dictionary<string, object?>;
    }

    private static bool TryExtractListCountValue(object? value, out string item, out string countText)
    {
        if (value is null)
        {
            item = string.Empty;
            countText = string.Empty;
            return false;
        }

        if (value is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            item = element.TryGetProperty("item", out var itemElement) && itemElement.ValueKind == System.Text.Json.JsonValueKind.String
                ? itemElement.GetString() ?? string.Empty
                : string.Empty;

            countText = element.TryGetProperty("count", out var countElement)
                ? countElement.ValueKind == System.Text.Json.JsonValueKind.String
                    ? countElement.GetString() ?? string.Empty
                    : countElement.GetRawText()
                : string.Empty;
            return true;
        }

        if (value is IDictionary<string, object?> dict)
        {
            item = dict.TryGetValue("item", out var rawItem) ? rawItem?.ToString() ?? string.Empty : string.Empty;
            countText = dict.TryGetValue("count", out var rawCount) ? rawCount?.ToString() ?? string.Empty : string.Empty;
            return true;
        }

        item = string.Empty;
        countText = string.Empty;
        return false;
    }

    private static double ParseCount(string text)
    {
        return double.TryParse(text, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0d;
    }
}
