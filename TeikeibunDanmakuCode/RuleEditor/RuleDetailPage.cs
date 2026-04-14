using Godot;
using TeikeibunDanmaku.Core.Condition;
using TeikeibunDanmaku.Core.Rules;
using TeikeibunDanmaku.RuleEditor.I18n;
using TeikeibunDanmaku.RuleEditor.Model;

namespace TeikeibunDanmaku.RuleEditor;

public sealed partial class RuleDetailPage : PanelContainer
{
    private LineEdit _ruleIdInput = null!;
    private OptionButton _timepointSelect = null!;
    private ItemList _messageList = null!;
    private LineEdit _messageInput = null!;
    private Label _conditionSummary = null!;
    private Label _statusLabel = null!;

    private ConditionDto _currentCondition = null!;

    public event Action<RuleDto>? DoneRequested;
    public event Action? CancelRequested;
    public event Action? EditConditionRequested;

    public override void _Ready()
    {
        _ruleIdInput = GetNode<LineEdit>("%RuleIdInput");
        _timepointSelect = GetNode<OptionButton>("%TimepointSelect");
        _messageList = GetNode<ItemList>("%MessageList");
        _messageInput = GetNode<LineEdit>("%MessageInput");
        _conditionSummary = GetNode<Label>("%ConditionSummaryLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");

        GetNode<Label>("%TitleLabel").Text = EditorLoc.T("detail.title");
        GetNode<Label>("%RuleIdLabel").Text = EditorLoc.T("detail.rule_id");
        GetNode<Label>("%TimepointLabel").Text = EditorLoc.T("detail.timepoint");
        GetNode<Label>("%MessagesLabel").Text = EditorLoc.T("detail.messages");
        _messageInput.PlaceholderText = EditorLoc.T("detail.message_placeholder");

        GetNode<Button>("%AddMessageButton").Text = EditorLoc.T("detail.msg_add");
        GetNode<Button>("%UpdateMessageButton").Text = EditorLoc.T("detail.msg_update");
        GetNode<Button>("%RemoveMessageButton").Text = EditorLoc.T("detail.msg_remove");
        GetNode<Button>("%MoveMsgUpButton").Text = EditorLoc.T("detail.msg_up");
        GetNode<Button>("%MoveMsgDownButton").Text = EditorLoc.T("detail.msg_down");
        GetNode<Button>("%EditConditionButton").Text = EditorLoc.T("detail.edit_condition");
        GetNode<Button>("%DoneButton").Text = EditorLoc.T("common.done");
        GetNode<Button>("%CancelButton").Text = EditorLoc.T("common.cancel");

        _messageInput.TextSubmitted += text => AddMessage(text);
        GetNode<Button>("%AddMessageButton").Pressed += () => AddMessage(_messageInput.Text);
        GetNode<Button>("%UpdateMessageButton").Pressed += UpdateSelectedMessage;
        GetNode<Button>("%RemoveMessageButton").Pressed += RemoveSelectedMessage;
        GetNode<Button>("%MoveMsgUpButton").Pressed += () => MoveMessage(-1);
        GetNode<Button>("%MoveMsgDownButton").Pressed += () => MoveMessage(1);
        GetNode<Button>("%EditConditionButton").Pressed += () => EditConditionRequested?.Invoke();
        GetNode<Button>("%DoneButton").Pressed += EmitDone;
        GetNode<Button>("%CancelButton").Pressed += () => CancelRequested?.Invoke();
    }

    public void BeginEdit(RuleDto rule, IReadOnlyList<string> timepoints, string status)
    {
        _ruleIdInput.Text = rule.RuleId;
        _timepointSelect.Clear();

        var selectedIndex = 0;
        for (var i = 0; i < timepoints.Count; i++)
        {
            _timepointSelect.AddItem(timepoints[i]);
            if (string.Equals(timepoints[i], rule.Timepoint, StringComparison.Ordinal))
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
        return _timepointSelect.Selected >= 0
            ? _timepointSelect.GetItemText(_timepointSelect.Selected)
            : string.Empty;
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
        _conditionSummary.Text = BuildConditionSummary(_currentCondition, 0);
    }

    private static string BuildConditionSummary(ConditionDto condition, int depth)
    {
        var indent = new string(' ', depth * 2);
        if (condition.Type is ConditionType.And or ConditionType.Or)
        {
            var children = condition.Conditions?.ToArray() ?? [];
            var lines = new List<string> { $"{indent}{condition.Type.ToUpperInvariant()} ({children.Length})" };
            lines.AddRange(children.Select(child => BuildConditionSummary(child, depth + 1)));
            return string.Join("\n", lines);
        }

        return $"{indent}{condition.Type} key={condition.Key} value={condition.Value}";
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
