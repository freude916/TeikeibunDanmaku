namespace TeikeibunDanmaku.Timepoints;

public static class TimepointBus
{
    public static event Action<Timepoint>? Published;

    public static void Publish(Timepoint timepoint)
    {
        ArgumentNullException.ThrowIfNull(timepoint);
        Published?.Invoke(timepoint);
    }
}
