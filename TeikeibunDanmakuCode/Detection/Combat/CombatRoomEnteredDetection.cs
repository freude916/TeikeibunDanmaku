using System.Collections;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.Detection.Combat;

[HarmonyPatch(typeof(CombatManager), "SetUpCombat")]
public static class CombatRoomEnteredDetection
{
    [HarmonyPostfix]
    public static void SetUpCombatPostfix(CombatManager __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        try
        {
            var enemyNames = ReadEnemyNames(__instance);
            CombatRoomEnteredTimepoint.FromEnemies(enemyNames).Publish();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"CombatRoomEnteredDetection failed. ExceptionType={ex.GetType().FullName}, HResult=0x{ex.HResult:X8}"
            );
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                MainFile.Logger.Error(ex.StackTrace);
        }
    }

    private static IReadOnlyList<string> ReadEnemyNames(object source)
    {
        var enemies = GetMemberValue(source, "Enemies")
                      ?? GetMemberValue(source, "EnemyCreatures")
                      ?? GetMemberValue(source, "Monsters")
                      ?? GetMemberValue(source, "MonsterCreatures")
                      ?? GetMemberValue(source, "AliveEnemies");

        if (TryReadEnemyNamesFromEnumerable(enemies, out var namesFromRoot))
            return namesFromRoot;

        var state = GetMemberValue(source, "State")
                    ?? GetMemberValue(source, "CombatState");
        if (state != null && TryReadEnemyNamesFromEnumerable(GetMemberValue(state, "Enemies"), out var namesFromStateEnemies))
            return namesFromStateEnemies;

        var encounter = GetMemberValue(state ?? source, "Encounter");
        if (encounter != null && TryReadEnemyNamesFromEnumerable(GetMemberValue(encounter, "Monsters"), out var namesFromEncounter))
            return namesFromEncounter;

        return [];
    }

    private static bool TryReadEnemyNamesFromEnumerable(object? source, out IReadOnlyList<string> names)
    {
        names = [];
        if (source is not IEnumerable enumerable || source is string)
            return false;

        var result = new List<string>();
        foreach (var enemy in enumerable)
        {
            var name = TryResolveEnemyName(enemy);
            if (!string.IsNullOrWhiteSpace(name))
                result.Add(name);
        }

        if (result.Count == 0)
            return false;

        names = result;
        return true;
    }

    private static string TryResolveEnemyName(object? enemy)
    {
        if (enemy == null)
            return string.Empty;

        var model = GetMemberValue(enemy, "Model")
                    ?? GetMemberValue(enemy, "MonsterModel")
                    ?? GetMemberValue(enemy, "DataModel")
                    ?? enemy;

        var nameValue = GetMemberValue(model, "TitleLocString")
                        ?? GetMemberValue(model, "NameLocString")
                        ?? GetMemberValue(model, "Title")
                        ?? GetMemberValue(model, "Name");

        var name = FormatLocString(nameValue);
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        var modelId = GetMemberValue(model, "Id")
                      ?? GetMemberValue(enemy, "Id");
        return (GetMemberValue(modelId, "Entry") as string) ?? (modelId as string) ?? string.Empty;
    }

    private static string FormatLocString(object? locOrText)
    {
        if (locOrText == null)
            return string.Empty;

        if (locOrText is string text)
            return text;

        var getFormattedText = locOrText.GetType().GetMethod(
            "GetFormattedText",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            Type.EmptyTypes,
            modifiers: null
        );
        if (getFormattedText != null && getFormattedText.ReturnType == typeof(string))
            return (string?)getFormattedText.Invoke(locOrText, null) ?? string.Empty;

        return locOrText.ToString() ?? string.Empty;
    }

    private static object? GetMemberValue(object target, string memberName)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var targetType = target.GetType();

        var prop = targetType.GetProperty(memberName, flags);
        if (prop != null)
            return prop.GetValue(target);

        var field = targetType.GetField(memberName, flags);
        return field?.GetValue(target);
    }
}
