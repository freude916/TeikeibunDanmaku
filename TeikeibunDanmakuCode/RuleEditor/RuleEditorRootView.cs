using Godot;
using TeikeibunDanmaku.Core.Rules;
using TeikeibunDanmaku.RuleEditor.I18n;
using TeikeibunDanmaku.RuleEditor.Model;
using TeikeibunDanmaku.RuleEditor.Services;

namespace TeikeibunDanmaku.RuleEditor;

public sealed partial class RuleEditorRootView : Control
{
    private const string ConditionDialogScenePath = "res://TeikeibunDanmaku/scenes/Dialogs/ConditionEditorDialog.tscn";

    private readonly RuleFileGateway _fileGateway = new();
    private readonly RuleRuntimeGateway _runtimeGateway = new();
    private readonly RuleValidationService _validationService = new();
    private readonly ConditionSchemaService _schemaService = new();

    private RuleFilePage _filePage = null!;
    private RuleListPage _listPage = null!;
    private RuleDetailPage _detailPage = null!;
    private Control _dialogLayer = null!;
    private UnsavedDialog _unsavedDialog = null!;

    private readonly List<RuleDto> _rules = [];
    private readonly List<ConditionEditorDialog> _dialogStack = [];

    private string? _currentFilePath;
    private bool _isDirty;
    private string _status = string.Empty;

    private int _editingIndex = -1;

    public override void _Ready()
    {
        _filePage = GetNode<RuleFilePage>("%FilePage");
        _listPage = GetNode<RuleListPage>("%ListPage");
        _detailPage = GetNode<RuleDetailPage>("%DetailPage");
        _dialogLayer = GetNode<Control>("%DialogLayer");
        _unsavedDialog = GetNode<UnsavedDialog>("%UnsavedDialog");

        _filePage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _listPage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _detailPage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _filePage.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _filePage.SizeFlagsVertical = SizeFlags.ExpandFill;
        _listPage.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _listPage.SizeFlagsVertical = SizeFlags.ExpandFill;
        _detailPage.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _detailPage.SizeFlagsVertical = SizeFlags.ExpandFill;

        GetNode<ColorRect>("%Backdrop").Color = new Color(0f, 0f, 0f, 0.58f);
        Visible = false;

        _filePage.OpenRequested += OpenFile;
        _filePage.CreateRequested += CreateAndOpenFile;

        _listPage.AddRequested += StartCreateRule;
        _listPage.EditRequested += StartEditRule;
        _listPage.DeleteRequested += DeleteRule;
        _listPage.MoveUpRequested += index => MoveRule(index, -1);
        _listPage.MoveDownRequested += index => MoveRule(index, 1);
        _listPage.SaveRequested += () => SaveAndReload();
        _listPage.SwitchFileRequested += ShowFilePage;
        _listPage.CloseRequested += OnCloseRequested;

        _detailPage.DoneRequested += CommitRuleDraft;
        _detailPage.CancelRequested += ShowRuleListPage;
        _detailPage.EditConditionRequested += OpenRootConditionDialog;

        _unsavedDialog.SaveRequested += () =>
        {
            if (SaveAndReload())
            {
                HideEditor();
            }
            else
            {
                _unsavedDialog.Visible = false;
            }
        };
        _unsavedDialog.DiscardRequested += () =>
        {
            _unsavedDialog.Visible = false;
            HideEditor();
        };
        _unsavedDialog.ReturnRequested += () => _unsavedDialog.Visible = false;
        _unsavedDialog.Visible = false;

        ShowFilePage();
    }

    public void Toggle()
    {
        if (Visible)
        {
            OnCloseRequested();
            return;
        }

        Visible = true;
        ShowFilePage();
    }

    private void HideEditor()
    {
        CloseAllConditionDialogs();
        Visible = false;
    }

    private void OnCloseRequested()
    {
        if (_isDirty)
        {
            _unsavedDialog.Visible = true;
            return;
        }

        HideEditor();
    }

    private void ShowFilePage()
    {
        CloseAllConditionDialogs();
        _filePage.Visible = true;
        _listPage.Visible = false;
        _detailPage.Visible = false;

        _filePage.SetData(_fileGateway.ListRuleFiles(), _status);
    }

    private void ShowRuleListPage()
    {
        CloseAllConditionDialogs();
        _filePage.Visible = false;
        _listPage.Visible = true;
        _detailPage.Visible = false;

        _listPage.SetData(_currentFilePath, _rules, _isDirty, _status);
    }

    private void ShowRuleDetailPage(RuleDto draft)
    {
        CloseAllConditionDialogs();
        _filePage.Visible = false;
        _listPage.Visible = false;
        _detailPage.Visible = true;

        _detailPage.BeginEdit(draft, _schemaService.ListTimepoints(), _status);
    }

    private void OpenFile(string filePath)
    {
        try
        {
            _rules.Clear();
            _rules.AddRange(_fileGateway.LoadDtos(filePath).Select(rule => rule.Clone()));
            _currentFilePath = filePath;
            _isDirty = false;
            _status = $"{EditorLoc.T("status.loaded")} {Path.GetFileName(filePath)} ({_rules.Count})";
            ShowRuleListPage();
        }
        catch (Exception ex)
        {
            _status = $"{EditorLoc.T("status.load_failed")} {ex.Message}";
            ShowFilePage();
        }
    }

    private void CreateAndOpenFile(string fileName)
    {
        try
        {
            var filePath = _fileGateway.EnsureFilePath(fileName);
            if (!File.Exists(filePath))
            {
                _fileGateway.SaveDtos(filePath, []);
            }

            OpenFile(filePath);
        }
        catch (Exception ex)
        {
            _status = $"{EditorLoc.T("status.create_failed")} {ex.Message}";
            ShowFilePage();
        }
    }

    private void StartCreateRule()
    {
        _editingIndex = -1;
        var timepoint = _schemaService.ListTimepoints().FirstOrDefault().Id;
        if (string.IsNullOrWhiteSpace(timepoint))
        {
            _status = "未配置任何时间点。";
            ShowRuleListPage();
            return;
        }

        var draft = new RuleDto
        {
            RuleId = GenerateRuleId(timepoint),
            Timepoint = timepoint,
            Messages = [],
            Condition = _schemaService.CreateDefaultLeaf(timepoint)
        };

        _status = EditorLoc.T("status.edit_new_rule");
        ShowRuleDetailPage(draft);
    }

    private void StartEditRule(int index)
    {
        if (!TryGetRule(index, out var rule))
        {
            return;
        }

        _editingIndex = index;
        _status = $"{EditorLoc.T("status.edit_rule")} {rule.RuleId}";
        ShowRuleDetailPage(rule.Clone());
    }

    private void DeleteRule(int index)
    {
        if (!TryGetRule(index, out _))
        {
            return;
        }

        _rules.RemoveAt(index);
        _isDirty = true;
        _status = EditorLoc.T("status.rule_deleted");
        ShowRuleListPage();
    }

    private void MoveRule(int index, int delta)
    {
        if (!TryGetRule(index, out var rule))
        {
            return;
        }

        var target = index + delta;
        if (target < 0 || target >= _rules.Count)
        {
            _status = EditorLoc.T("status.rule_move_out_of_range");
            ShowRuleListPage();
            return;
        }

        _rules.RemoveAt(index);
        _rules.Insert(target, rule);
        _isDirty = true;
        _status = EditorLoc.T("status.rule_moved");
        ShowRuleListPage();
    }

    private bool SaveAndReload()
    {
        if (string.IsNullOrWhiteSpace(_currentFilePath))
        {
            _status = EditorLoc.T("error.file_not_loaded");
            ShowRuleListPage();
            return false;
        }

        var result = _validationService.ValidateAll(_rules);
        if (!result.IsValid)
        {
            _status = BuildValidationMessage(result);
            ShowRuleListPage();
            return false;
        }

        try
        {
            _fileGateway.SaveDtos(_currentFilePath, _rules);
            _runtimeGateway.ReloadRulesFromFile(_currentFilePath);
            _isDirty = false;
            _status = $"{EditorLoc.T("status.saved")} {Path.GetFileName(_currentFilePath)}";
            ShowRuleListPage();
            return true;
        }
        catch (Exception ex)
        {
            _status = $"{EditorLoc.T("status.save_failed")} {ex.Message}";
            ShowRuleListPage();
            return false;
        }
    }

    private void CommitRuleDraft(RuleDto draft)
    {
        draft = draft.Clone();
        draft = new RuleDto
        {
            RuleId = draft.RuleId,
            Timepoint = draft.Timepoint,
            Messages = [.. draft.Messages],
            Condition = _schemaService.NormalizeNode(draft.Timepoint, draft.Condition)
        };

        var candidate = _rules.Select(rule => rule.Clone()).ToList();
        if (_editingIndex >= 0)
        {
            candidate[_editingIndex] = draft;
        }
        else
        {
            candidate.Add(draft);
        }

        var result = _validationService.ValidateAll(candidate);
        if (!result.IsValid)
        {
            _status = BuildValidationMessage(result);
            _detailPage.SetStatus(_status);
            return;
        }

        if (_editingIndex >= 0)
        {
            _rules[_editingIndex] = draft;
        }
        else
        {
            _rules.Add(draft);
        }

        _isDirty = true;
        _editingIndex = -1;
        _status = EditorLoc.T("status.rule_updated");
        ShowRuleListPage();
    }

    private void OpenRootConditionDialog()
    {
        OpenConditionDialog([], _detailPage.GetCondition());
    }

    private void OpenConditionDialog(IReadOnlyList<int> path, Core.Condition.ConditionDto node)
    {
        try
        {
            var scene = GD.Load<PackedScene>(ConditionDialogScenePath)
                        ?? throw new InvalidOperationException("Condition dialog scene not found.");

            var dialog = scene.Instantiate<ConditionEditorDialog>();
            dialog.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

            dialog.DoneRequested += OnConditionDone;
            dialog.CancelRequested += CloseTopConditionDialog;
            dialog.EditChildRequested += OnEditConditionChild;
            dialog.AddChildRequested += OnAddConditionChild;
            dialog.RemoveChildRequested += OnRemoveConditionChild;

            if (_dialogStack.Count > 0)
            {
                _dialogStack[^1].Visible = false;
            }

            _dialogStack.Add(dialog);
            _dialogLayer.AddChild(dialog);

            dialog.Initialize(
                _detailPage.GetSelectedTimepoint(),
                path,
                node,
                _schemaService.ListConditionTypes(),
                _status);
        }
        catch (Exception ex)
        {
            _status = $"condition.open.failed: {ex.Message}";
            _detailPage.SetStatus(_status);
            MainFile.Logger.Error($"Condition dialog open failed: {ex}");
        }
    }

    private void OnConditionDone(IReadOnlyList<int> path, Core.Condition.ConditionDto node)
    {
        var normalized = _schemaService.NormalizeNode(_detailPage.GetSelectedTimepoint(), node);
        var root = _detailPage.GetCondition().ReplaceAtPath(path, normalized);

        var result = _validationService.ValidateCondition(_detailPage.GetSelectedTimepoint(), root);
        if (!result.IsValid)
        {
            _status = BuildValidationMessage(result);
            _dialogStack[^1].SetStatus(_status);
            return;
        }

        if (path.Count == 0)
        {
            _detailPage.UpdateCondition(root);
            CloseAllConditionDialogs();
            return;
        }

        _detailPage.UpdateCondition(root);
        CloseTopConditionDialog();

        if (_dialogStack.Count > 0)
        {
            var parentPath = path.Take(path.Count - 1).ToArray();
            var childIndex = path[^1];
            if (_detailPage.GetCondition().TryGetNodeByPath(parentPath, out var parentNode))
            {
                var visibleParent = _dialogStack[^1];
                var childNode = parentNode.Conditions?.ToArray().ElementAtOrDefault(childIndex);
                if (childNode != null)
                {
                    visibleParent.ReplaceChild(childIndex, childNode);
                }

                visibleParent.SetStatus(EditorLoc.T("status.condition_child_updated"));
            }
        }
    }

    private void OnEditConditionChild(IReadOnlyList<int> path, int childIndex)
    {
        var current = _detailPage.GetCondition();
        if (!current.TryGetNodeByPath(path, out var parentNode))
        {
            return;
        }

        var children = parentNode.Conditions?.ToArray() ?? [];
        if (childIndex < 0 || childIndex >= children.Length)
        {
            return;
        }

        var childPath = path.Concat([childIndex]).ToArray();
        OpenConditionDialog(childPath, children[childIndex]);
    }

    private void OnAddConditionChild(IReadOnlyList<int> path)
    {
        if (_dialogStack.Count == 0)
        {
            return;
        }

        var child = _schemaService.CreateDefaultLeaf(_detailPage.GetSelectedTimepoint());
        _dialogStack[^1].AddChild(child);
    }

    private void OnRemoveConditionChild(IReadOnlyList<int> path, int childIndex)
    {
        if (_dialogStack.Count == 0)
        {
            return;
        }

        _dialogStack[^1].RemoveChild(childIndex);
    }

    private void CloseTopConditionDialog()
    {
        if (_dialogStack.Count == 0)
        {
            return;
        }

        var top = _dialogStack[^1];
        _dialogStack.RemoveAt(_dialogStack.Count - 1);
        top.QueueFree();

        if (_dialogStack.Count > 0)
        {
            _dialogStack[^1].Visible = true;
        }
    }

    private void CloseAllConditionDialogs()
    {
        foreach (var dialog in _dialogStack)
        {
            dialog.QueueFree();
        }

        _dialogStack.Clear();
    }

    private bool TryGetRule(int index, out RuleDto rule)
    {
        if (index < 0 || index >= _rules.Count)
        {
            rule = null!;
            _status = EditorLoc.T("error.invalid_index");
            ShowRuleListPage();
            return false;
        }

        rule = _rules[index];
        return true;
    }

    private string GenerateRuleId(string timepointId)
    {
        var prefix = $"{timepointId.Replace('.', '_')}_";
        var used = _rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 1; i < 10000; i++)
        {
            var id = $"{prefix}{i:D3}";
            if (!used.Contains(id))
            {
                return id;
            }
        }

        return $"{prefix}{Guid.NewGuid():N}";
    }

    private static string BuildValidationMessage(ValidationResult result)
    {
        return string.IsNullOrWhiteSpace(result.Detail)
            ? result.ErrorCode
            : $"{result.ErrorCode}: {result.Detail}";
    }
}
