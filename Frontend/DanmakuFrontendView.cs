using Godot;
using TeikeibunDanmaku.Core;
using Timer = Godot.Timer;

namespace TeikeibunDanmaku.Frontend;

public sealed class DanmakuFrontendView : IDisposable
{
    private readonly PanelContainer _inputPanel;
    private readonly LineEdit _inputField;
    private readonly Timer _layoutTimer;
    private readonly DanmakuOverlayController _overlayController;
    private readonly Control _overlayRoot;
    private readonly Control _root;
    private readonly Button _sendButton;
    private bool _isDisposed;

    public DanmakuFrontendView(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);

        _root = root;
        _root.MouseFilter = Control.MouseFilterEnum.Ignore;

        _overlayRoot = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _root.AddChild(_overlayRoot);
        _overlayController = new DanmakuOverlayController(_overlayRoot);
        DanmakuEventBus.DanmakuRequested += OnDanmakuRequested;

        _inputPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(
                DanmakuFrontendConfig.InputPanelWidthPx,
                DanmakuFrontendConfig.InputPanelHeightPx
            )
        };

        var panelStyle = new StyleBoxFlat
        {
            BgColor = DanmakuFrontendConfig.InputPanelBackground,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        _inputPanel.AddThemeStyleboxOverride("panel", panelStyle);
        _root.AddChild(_inputPanel);

        var inputLayout = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        _inputPanel.AddChild(inputLayout);

        _inputField = new LineEdit
        {
            PlaceholderText = "输入测试弹幕后按回车",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _inputField.TextSubmitted += OnTextSubmitted;
        inputLayout.AddChild(_inputField);

        _sendButton = new Button
        {
            Text = "发送"
        };
        _sendButton.Pressed += OnSendPressed;
        inputLayout.AddChild(_sendButton);

        _layoutTimer = new Timer
        {
            WaitTime = 1.0 / 20.0,
            OneShot = false,
            Autostart = true,
            ProcessMode = Node.ProcessModeEnum.Always
        };
        _layoutTimer.Timeout += OnLayoutTick;
        _root.AddChild(_layoutTimer);
        UpdateLayout();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _inputField.TextSubmitted -= OnTextSubmitted;
        _sendButton.Pressed -= OnSendPressed;
        _layoutTimer.Timeout -= OnLayoutTick;
        DanmakuEventBus.DanmakuRequested -= OnDanmakuRequested;
        if (GodotObject.IsInstanceValid(_layoutTimer))
            _layoutTimer.QueueFree();

        _overlayController.Dispose();
    }

    private void OnLayoutTick()
    {
        UpdateLayout();
    }

    private void OnTextSubmitted(string text)
    {
        SubmitDanmaku(text);
    }

    private void OnSendPressed()
    {
        SubmitDanmaku(_inputField.Text);
    }

    private void SubmitDanmaku(string text)
    {
        EnqueueDanmaku(text, isAuto: false);
    }

    private void OnDanmakuRequested(string text)
    {
        EnqueueDanmaku(text, isAuto: true);
    }

    private void EnqueueDanmaku(string text, bool isAuto)
    {
        if (_isDisposed || string.IsNullOrWhiteSpace(text))
            return;

        var item = isAuto
            ? DanmakuItemFactory.CreateForAutoDanmaku(text)
            : DanmakuItemFactory.CreateForTestInput(text);
        _overlayController.Enqueue(item);

        if (!isAuto)
        {
            _inputField.Text = string.Empty;
            _inputField.CallDeferred(Control.MethodName.GrabFocus);
        }
    }

    private void UpdateLayout()
    {
        if (_isDisposed)
            return;

        var viewportSize = _root.GetViewportRect().Size;
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f)
            return;

        var margin = DanmakuFrontendConfig.InputPanelMarginPx;
        var maxWidth = Mathf.Max(180f, viewportSize.X - margin * 2f);
        var width = Mathf.Min(DanmakuFrontendConfig.InputPanelWidthPx, maxWidth);

        _inputPanel.Size = new Vector2(width, DanmakuFrontendConfig.InputPanelHeightPx);
        _inputPanel.Position = new Vector2(
            margin,
            Mathf.Max(margin, viewportSize.Y - margin - _inputPanel.Size.Y)
        );
    }
}
