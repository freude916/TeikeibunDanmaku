using MegaCrit.Sts2.Core.Localization;

namespace TeikeibunDanmaku.RuleEditor.I18n;

public static class EditorLoc
{
    private const string Table = "gameplay_ui";
    private const string Prefix = "TEIKEIBUN.rule_editor.";

    public static string T(string key)
    {
        try
        {
            return new LocString(Table, Prefix + key).GetFormattedText();
        }
        catch
        {
            return Prefix + key;
        }
    }
}
