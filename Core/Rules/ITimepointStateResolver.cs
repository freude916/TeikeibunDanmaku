namespace TeikeibunDanmaku.Core.Rules;

public interface ITimepointStateResolver
{
    Type ResolveStateType(string timepointId);
}
