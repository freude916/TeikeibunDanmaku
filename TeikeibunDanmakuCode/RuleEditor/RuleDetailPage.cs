using Godot;
using TeikeibunDanmaku.Core.Message;
using TeikeibunDanmaku.Core.Condition;
using TeikeibunDanmaku.Core.Rules;
using TeikeibunDanmaku.RuleEditor.Model;
using TeikeibunDanmaku.RuleEditor.Services;
using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.RuleEditor;

public sealed partial class RuleDetailPage : PanelContainer
{
    private const string TemplateSectionCatchphrase = "口癖模板";
    private const string TemplateSectionField = "字段模板";

    private LineEdit _ruleIdInput = null!;
    private OptionButton _timepointSelect = null!;
    private ItemList _messageList = null!;
    private LineEdit _messageInput = null!;
    private MenuButton _templateMenuButton = null!;
    private Label _conditionSummary = null!;
    private Label _statusLabel = null!;

    private ConditionDto _currentCondition = null!;
    private readonly ConditionSchemaService _schemaService = new();
    private readonly Dictionary<int, string> _messageTemplateTokens = new();

    public event Action<RuleDto>? DoneRequested;
    public event Action? CancelRequested;
    public event Action? EditConditionRequested;

    public override void _Ready()
    {
        _ruleIdInput = GetNode<LineEdit>("%RuleIdInput");
        _timepointSelect = GetNode<OptionButton>("%TimepointSelect");
        _messageList = GetNode<ItemList>("%MessageList");
        _messageInput = GetNode<LineEdit>("%MessageInput");
        _templateMenuButton = GetNode<MenuButton>("%TemplateMenuButton");
        _conditionSummary = GetNode<Label>("%ConditionSummaryLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");

        _messageInput.TextSubmitted += text => AddMessage(text);
        GetNode<Button>("%AddMessageButton").Pressed += () => AddMessage(_messageInput.Text);
        GetNode<Button>("%UpdateMessageButton").Pressed += UpdateSelectedMessage;
        GetNode<Button>("%RemoveMessageButton").Pressed += RemoveSelectedMessage;
        GetNode<Button>("%MoveMsgUpButton").Pressed += () => MoveMessage(-1);
        GetNode<Button>("%MoveMsgDownButton").Pressed += () => MoveMessage(1);
        GetNode<Button>("%EditConditionButton").Pressed += () => EditConditionRequested?.Invoke();
        GetNode<Button>("%DoneButton").Pressed += EmitDone;
        GetNode<Button>("%CancelButton").Pressed += () => CancelRequested?.Invoke();

        var popup = _templateMenuButton.GetPopup();
        popup.AboutToPopup += RebuildMessageTemplateMenu;
        popup.IdPressed += OnTemplateMenuItemPressed;
    }

    public void BeginEdit(RuleDto rule, IReadOnlyList<TimepointDescriptor> timepoints, string status)
    {
        _ruleIdInput.Text = rule.RuleId;
        _timepointSelect.Clear();

        var selectedIndex = 0;
        for (var i = 0; i < timepoints.Count; i++)
        {
            _timepointSelect.AddItem(timepoints[i].DisplayName);
            _timepointSelect.SetItemMetadata(i, timepoints[i].Id);
            if (string.Equals(timepoints[i].Id, rule.Timepoint, StringComparison.Ordinal))
            {
                selectedIndex = i;
            }
        }

        _timepointSelect.Select(selectedIndex);

        _messageList.Clear();
        foreach (var message in rule.Messages)
        {
            _messageList.AddItem(message);
        }

        _currentCondition = rule.Condition.Clone();
        _statusLabel.Text = status;
        RebuildConditionSummary();
    }

    public void UpdateCondition(ConditionDto condition)
    {
        _currentCondition = condition.Clone();
        RebuildConditionSummary();
    }

    public ConditionDto GetCondition() => _currentCondition.Clone();

    public string GetSelectedTimepoint()
    {
        if (_timepointSelect.Selected < 0 || _timepointSelect.Selected >= _timepointSelect.ItemCount)
        {
            return string.Empty;
        }

        var metadata = _timepointSelect.GetItemMetadata(_timepointSelect.Selected);
        return metadata.VariantType == Variant.Type.String ? metadata.AsString() : string.Empty;
    }

    public void SetStatus(string status)
    {
        _statusLabel.Text = status;
    }

    private void EmitDone()
    {
        var messages = Enumerable.Range(0, _messageList.ItemCount)
            .Select(index => _messageList.GetItemText(index).Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();

        var rule = new RuleDto
        {
            RuleId = _ruleIdInput.Text.Trim(),
            Timepoint = GetSelectedTimepoint(),
            Messages = messages,
            Condition = _currentCondition.Clone()
        };

        DoneRequested?.Invoke(rule);
    }

    private void RebuildConditionSummary()
    {
        _conditionSummary.Text = BuildConditionSummary(_currentCondition, 0, GetSelectedTimepoint());
    }

    private string BuildConditionSummary(ConditionDto condition, int depth, string timepointId)
    {
        var indent = new string(' ', depth * 2);
        if (condition.Type is ConditionType.CondAnd or ConditionType.CondOr)
        {
            var children = condition.Conditions?.ToArray() ?? [];
            var lines = new List<string> { $"{indent}{_schemaService.GetConditionTypeDisplayName(condition.Type)} ({children.Length})" };
            lines.AddRange(children.Select(child => BuildConditionSummary(child, depth + 1, timepointId)));
            return string.Join("\n", lines);
        }

        var typeName = _schemaService.GetConditionTypeDisplayName(condition.Type);
        var keyName = _schemaService.GetFieldDisplayName(timepointId, condition.Key);
        return $"{indent}{typeName} key={keyName} value={condition.Value}";
    }

    private void AddMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _messageList.AddItem(text.Trim());
        _messageInput.Text = string.Empty;
    }

    private void RebuildMessageTemplateMenu()
    {
        var popup = _templateMenuButton.GetPopup();
        popup.Clear();
        _messageTemplateTokens.Clear();

        var nextId = 1;
        var hasAny = false;

        var catchphraseTemplates = CatchphraseRegistry.ListTemplates();
        if (catchphraseTemplates.Count > 0)
        {
            popup.AddSeparator(TemplateSectionCatchphrase);
            foreach (var template in catchphraseTemplates)
            {
                var label = $"{template.DisplayName}  {template.Token}";
                popup.AddItem(label, nextId);
                _messageTemplateTokens[nextId] = template.Token;
                nextId++;
                hasAny = true;
            }
        }

        var timepointId = GetSelectedTimepoint();
        var fieldTemplates = _schemaService.GetAllowedFieldDescriptors(timepointId, ConditionType.Eq);
        if (fieldTemplates.Count > 0)
        {
            popup.AddSeparator(TemplateSectionField);
            foreach (var field in fieldTemplates)
            {
                var token = "${" + field.Name + "}";
                var label = $"{field.DisplayName}  {token}";
                popup.AddItem(label, nextId);
                _messageTemplateTokens[nextId] = token;
                nextId++;
                hasAny = true;
            }
        }

        if (!hasAny)
        {
            popup.AddItem("暂无可插入模板");
            popup.SetItemDisabled(0, true);
        }
    }

    private void OnTemplateMenuItemPressed(long id)
    {
        if (!_messageTemplateTokens.TryGetValue((int)id, out var token))
        {
            return;
        }

        InsertMessageTemplateToken(token);
    }

    private void InsertMessageTemplateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var source = _messageInput.Text;
        var caret = Math.Clamp(_messageInput.CaretColumn, 0, source.Length);
        _messageInput.Text = source.Insert(caret, token);
        _messageInput.CaretColumn = caret + token.Length;
        _messageInput.GrabFocus();
    }

    private void UpdateSelectedMessage()
    {
        var selected = _messageList.GetSelectedItems();
        if (selected.Length == 0 || string.IsNullOrWhiteSpace(_messageInput.Text))
        {
            return;
        }

        _messageList.SetItemText(selected[0], _messageInput.Text.Trim());
    }

    private void RemoveSelectedMessage()
    {
        var selected = _messageList.GetSelectedItems();
        if (selected.Length == 0)
        {
            return;
        }

        _messageList.RemoveItem(selected[0]);
    }

    private void MoveMessage(int delta)
    {
        var selected = _messageList.GetSelectedItems();
        if (selected.Length == 0)
        {
            return;
        }

        var oldIndex = selected[0];
        var newIndex = oldIndex + delta;
        if (newIndex < 0 || newIndex >= _messageList.ItemCount)
        {
            return;
        }

        var items = Enumerable.Range(0, _messageList.ItemCount)
            .Select(index => _messageList.GetItemText(index))
            .ToList();

        var moving = items[oldIndex];
        items.RemoveAt(oldIndex);
        items.Insert(newIndex, moving);

        _messageList.Clear();
        foreach (var item in items)
        {
            _messageList.AddItem(item);
        }

        _messageList.Select(newIndex);
    }
}
