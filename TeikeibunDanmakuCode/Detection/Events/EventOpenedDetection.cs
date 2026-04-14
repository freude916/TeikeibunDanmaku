using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.Detection.Events;

[HarmonyPatch(typeof(NEventRoom), "SetOptions")]
public static class EventOpenedDetection
{
    [HarmonyPostfix]
    public static void SetOptionsPostfix(NEventRoom __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        try
        {
            if (!TryBuildEventPayload(__instance, out var eventId, out var eventName))
                return;

            EventSeenTimepoint.From(eventId, eventName).Publish();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"EventOpenedDetection failed. ExceptionType={ex.GetType().FullName}, HResult=0x{ex.HResult:X8}"
            );
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                MainFile.Logger.Error(ex.StackTrace);
        }
    }

    private static bool TryBuildEventPayload(NEventRoom eventRoom, out string eventId, out string eventName)
    {
        eventId = string.Empty;
        eventName = string.Empty;

        var eventModel = GetMemberValue(eventRoom, "EventModel")
            ?? GetMemberValue(eventRoom, "Model")
            ?? GetMemberValue(eventRoom, "CurrentEventModel")
            ?? GetMemberValue(eventRoom, "CurrentEvent")
            ?? GetMemberValue(eventRoom, "Event");

        if (eventModel == null)
            return false;

        var modelId = GetMemberValue(eventModel, "Id");
        eventId = (GetMemberValue(modelId, "Entry") as string) ?? (modelId as string) ?? string.Empty;

        var nameValue = GetMemberValue(eventModel, "TitleLocString")
            ?? GetMemberValue(eventModel, "Title")
            ?? GetMemberValue(eventModel, "NameLocString")
            ?? GetMemberValue(eventModel, "Name");

        eventName = FormatLocString(nameValue);
        return !string.IsNullOrWhiteSpace(eventId) && !string.IsNullOrWhiteSpace(eventName);
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
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var targetType = target.GetType();

        var prop = targetType.GetProperty(memberName, Flags);
        if (prop != null)
            return prop.GetValue(target);

        var field = targetType.GetField(memberName, Flags);
        return field?.GetValue(target);
    }
}
