using Godot;

namespace TeikeibunDanmaku.RuleEditor;

public sealed partial class RuleFilePage : PanelContainer
{
    private ItemList _fileList = null!;
    private LineEdit _fileNameInput = null!;
    private Label _statusLabel = null!;

    public event Action<string>? OpenRequested;
    public event Action<string>? CreateRequested;

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

        _fileList = GetNode<ItemList>("%FileList");
        _fileNameInput = GetNode<LineEdit>("%FileNameInput");
        _statusLabel = GetNode<Label>("%StatusLabel");

        _fileList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _fileList.SizeFlagsVertical = SizeFlags.ExpandFill;
        _fileNameInput.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        _fileList.ItemActivated += OnItemActivated;
        _fileNameInput.TextSubmitted += _ => EmitCreate();
        GetNode<Button>("%CreateButton").Pressed += EmitCreate;
        GetNode<Button>("%OpenButton").Pressed += EmitOpenSelected;
    }

    public void SetData(IReadOnlyList<string> files, string status)
    {
        _fileList.Clear();
        foreach (var file in files)
        {
            var index = _fileList.AddItem(Path.GetFileName(file));
            _fileList.SetItemMetadata(index, file);
        }

        _statusLabel.Text = status;
    }

    private void EmitOpenSelected()
    {
        var selected = _fileList.GetSelectedItems();
        if (selected.Length == 0)
        {
            return;
        }

        var filePath = _fileList.GetItemMetadata(selected[0]).AsString();
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            OpenRequested?.Invoke(filePath);
        }
    }

    private void OnItemActivated(long index)
    {
        var filePath = _fileList.GetItemMetadata((int)index).AsString();
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            OpenRequested?.Invoke(filePath);
        }
    }

    private void EmitCreate()
    {
        var fileName = _fileNameInput.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            CreateRequested?.Invoke(fileName);
        }
    }
}
