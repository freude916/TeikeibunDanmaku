using Godot;
using Timer = Godot.Timer;

namespace TeikeibunDanmaku.Display;

public sealed class DanmakuOverlayController : IDisposable
{
    private sealed class ActiveDanmaku
    {
        public ActiveDanmaku(Label label, float speedPxPerSecond)
        {
            Label = label;
            SpeedPxPerSecond = speedPxPerSecond;
        }

        public Label Label { get; }
        public float SpeedPxPerSecond { get; }
    }

    private readonly Control _host;
    private readonly Queue<DanmakuItem> _pendingDanmakus = new();
    private readonly List<List<ActiveDanmaku>> _tracks = [];
    private readonly Timer _tickTimer;
    private bool _isDisposed;
    private ulong _lastTickUsec;
    private Vector2 _viewportSize;

    public DanmakuOverlayController(Control host)
    {
        ArgumentNullException.ThrowIfNull(host);

        _host = host;
        _host.MouseFilter = Control.MouseFilterEnum.Ignore;

        UpdateViewportLayout(true);

        _tickTimer = new Timer
        {
            WaitTime = 1.0 / 60.0,
            OneShot = false,
            Autostart = true,
            ProcessMode = Node.ProcessModeEnum.Always
        };
        _tickTimer.Timeout += OnTick;
        _host.AddChild(_tickTimer);

        _lastTickUsec = Time.GetTicksUsec();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _tickTimer.Timeout -= OnTick;

        if (GodotObject.IsInstanceValid(_tickTimer))
            _tickTimer.QueueFree();

        ClearActiveDanmakus();
    }

    public void Enqueue(DanmakuItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _pendingDanmakus.Enqueue(item);
        TrySpawnPending();
    }

    private void OnTick()
    {
        if (_isDisposed)
            return;

        var nowUsec = Time.GetTicksUsec();
        var deltaSeconds = (nowUsec - _lastTickUsec) / 1_000_000f;
        _lastTickUsec = nowUsec;

        if (deltaSeconds <= 0f || deltaSeconds > 0.25f)
            deltaSeconds = (float)_tickTimer.WaitTime;

        UpdateViewportLayout(false);
        if (_viewportSize.X <= 0f || _viewportSize.Y <= 0f)
            return;

        ProcessFrame(deltaSeconds);
    }

    private void ProcessFrame(float deltaSeconds)
    {
        for (var trackIndex = 0; trackIndex < _tracks.Count; trackIndex++)
        {
            var track = _tracks[trackIndex];
            for (var i = track.Count - 1; i >= 0; i--)
            {
                var active = track[i];
                if (!GodotObject.IsInstanceValid(active.Label))
                {
                    track.RemoveAt(i);
                    continue;
                }

                active.Label.Position = active.Label.Position with
                {
                    X = active.Label.Position.X - active.SpeedPxPerSecond * deltaSeconds
                };

                if (active.Label.Position.X + active.Label.Size.X < -DanmakuFrontendConfig.DespawnPaddingPx)
                {
                    active.Label.QueueFree();
                    track.RemoveAt(i);
                }
            }
        }

        TrySpawnPending();
    }

    private void UpdateViewportLayout(bool force)
    {
        var viewportSize = _host.GetViewportRect().Size;
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f)
        {
            _viewportSize = Vector2.Zero;
            EnsureTrackCapacity();
            return;
        }

        if (!force && viewportSize == _viewportSize)
            return;

        _viewportSize = viewportSize;
        _host.Position = Vector2.Zero;
        _host.Size = viewportSize;

        EnsureTrackCapacity();
        RepositionExistingDanmakus();
        TrySpawnPending();
    }

    private void EnsureTrackCapacity()
    {
        var targetTrackCount = 0;
        if (_viewportSize.X > 0f && _viewportSize.Y > 0f)
        {
            var availableHeight = Mathf.Max(
                0f,
                _viewportSize.Y - DanmakuFrontendConfig.OverlayTopPaddingPx - DanmakuFrontendConfig.OverlayBottomPaddingPx
            );
            targetTrackCount = Mathf.Max(
                1,
                (int)MathF.Floor(availableHeight / DanmakuFrontendConfig.TrackHeightPx)
            );
        }

        while (_tracks.Count < targetTrackCount)
            _tracks.Add([]);

        if (_tracks.Count <= targetTrackCount)
            return;

        for (var trackIndex = targetTrackCount; trackIndex < _tracks.Count; trackIndex++)
        {
            foreach (var active in _tracks[trackIndex])
            {
                if (GodotObject.IsInstanceValid(active.Label))
                    active.Label.QueueFree();
            }
        }

        _tracks.RemoveRange(targetTrackCount, _tracks.Count - targetTrackCount);
    }

    private void RepositionExistingDanmakus()
    {
        for (var trackIndex = 0; trackIndex < _tracks.Count; trackIndex++)
        {
            var trackY = GetTrackY(trackIndex);
            foreach (var active in _tracks[trackIndex])
            {
                if (!GodotObject.IsInstanceValid(active.Label))
                    continue;

                active.Label.Position = new Vector2(active.Label.Position.X, trackY);
            }
        }
    }

    private void TrySpawnPending()
    {
        if (_tracks.Count == 0)
            return;

        while (_pendingDanmakus.TryPeek(out var next))
        {
            if (!TrySpawn(next))
                return;

            _pendingDanmakus.Dequeue();
        }
    }

    private bool TrySpawn(DanmakuItem item)
    {
        if (_viewportSize.X <= 0f || _viewportSize.Y <= 0f)
            return false;

        for (var trackIndex = 0; trackIndex < _tracks.Count; trackIndex++)
        {
            if (CanSpawnOnTrack(trackIndex))
            {
                SpawnOnTrack(item, trackIndex);
                return true;
            }
        }

        return false;
    }

    private bool CanSpawnOnTrack(int trackIndex)
    {
        var track = _tracks[trackIndex];
        RemoveInvalidEntries(track);

        if (track.Count == 0)
            return true;

        var tail = track[^1];
        var rightEdge = tail.Label.Position.X + tail.Label.Size.X;
        return rightEdge <= _viewportSize.X - DanmakuFrontendConfig.TrackSpacingPx;
    }

    private static void RemoveInvalidEntries(List<ActiveDanmaku> track)
    {
        for (var i = track.Count - 1; i >= 0; i--)
        {
            if (!GodotObject.IsInstanceValid(track[i].Label))
                track.RemoveAt(i);
        }
    }

    private void SpawnOnTrack(DanmakuItem item, int trackIndex)
    {
        var label = CreateLabel(item);
        _host.AddChild(label);

        var labelSize = label.GetCombinedMinimumSize();
        label.Size = labelSize;
        label.Position = new Vector2(
            _viewportSize.X + DanmakuFrontendConfig.SpawnPaddingPx,
            GetTrackY(trackIndex)
        );

        _tracks[trackIndex].Add(
            new ActiveDanmaku(label, item.GetScrollSpeedPxPerSecond())
        );
    }

    private static Label CreateLabel(DanmakuItem item)
    {
        var label = new Label
        {
            Text = item.Text,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        label.AddThemeFontSizeOverride("font_size", DanmakuFrontendConfig.DanmakuFontSize);
        label.AddThemeColorOverride("font_color", item.GetDisplayColor());

        return label;
    }

    private static float GetTrackY(int trackIndex)
    {
        return DanmakuFrontendConfig.OverlayTopPaddingPx + trackIndex * DanmakuFrontendConfig.TrackHeightPx;
    }

    private void ClearActiveDanmakus()
    {
        foreach (var track in _tracks)
        {
            foreach (var active in track)
            {
                if (GodotObject.IsInstanceValid(active.Label))
                    active.Label.QueueFree();
            }
        }

        _tracks.Clear();
    }
}
