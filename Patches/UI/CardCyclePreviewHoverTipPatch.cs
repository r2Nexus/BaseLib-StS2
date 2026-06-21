using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using BaseLib.Utils;

namespace BaseLib.Patches.UI;

internal sealed class CardCyclePreviewState
{
    public Control Owner = null!;

    public List<IHoverTip> OriginalTips = [];

    public HoverTipAlignment Alignment;

    public int LastVersion;
}

internal static class CardCyclePreviewPatchStorage
{
    public static readonly Dictionary<NHoverTipSet, CardCyclePreviewState> Active = [];

    public static readonly ConditionalWeakTable<CardHoverTip, InstancedCardTipMarker> InstancedCardTips = new();

    public sealed class InstancedCardTipMarker;
}

internal static class CardCyclePreviewPatchHelpers
{
    public static List<IHoverTip> ResolveAllForCycleSet(
        IEnumerable<IHoverTip> tips)
    {
        List<IHoverTip> resolvedTips = HoverTipResolver.ResolveAll(tips);

        foreach (IHoverTip tip in resolvedTips)
        {
            if (tip is CardHoverTip cardHoverTip)
            {
                CardCyclePreviewPatchStorage.InstancedCardTips.GetValue(
                    cardHoverTip,
                    _ => new CardCyclePreviewPatchStorage.InstancedCardTipMarker());
            }
        }

        return resolvedTips;
    }

    public static void ClearChildren(Node parent)
    {
        foreach (Node child in parent.GetChildren().OfType<Node>().ToList())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }
}

[HarmonyPatch(typeof(CardHoverTip), nameof(CardHoverTip.IsInstanced), MethodType.Getter)]
public static class CardCyclePreviewCardHoverTipInstancedPatch
{
    private static void Postfix(
        CardHoverTip __instance,
        ref bool __result)
    {
        if (CardCyclePreviewPatchStorage.InstancedCardTips.TryGetValue(__instance, out _))
            __result = true;
    }
}

[HarmonyPatch(
    typeof(NHoverTipSet),
    nameof(NHoverTipSet.CreateAndShow),
    [
        typeof(Control),
        typeof(IEnumerable<IHoverTip>),
        typeof(HoverTipAlignment)
    ])]
public static class NHoverTipSetCreateAndShowCardCyclePreviewPatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(
        ref IEnumerable<IHoverTip> hoverTips,
        out List<IHoverTip>? __state)
    {
        List<IHoverTip> originalTips = hoverTips.ToList();

        if (!HoverTipResolver.HasResolvingTip(originalTips))
        {
            __state = null;
            return;
        }

        __state = originalTips;
        hoverTips = CardCyclePreviewPatchHelpers.ResolveAllForCycleSet(originalTips);
    }

    private static void Postfix(
        Control owner,
        HoverTipAlignment alignment,
        NHoverTipSet? __result,
        List<IHoverTip>? __state)
    {
        if (__result == null || __state == null)
            return;

        CardCyclePreviewPatchStorage.Active[__result] = new CardCyclePreviewState
        {
            Owner = owner,
            OriginalTips = __state,
            Alignment = alignment,
            LastVersion = HoverTipResolver.GetVersionKey(__state)
        };
    }
}

[HarmonyPatch(typeof(NHoverTipSet), nameof(NHoverTipSet._Process))]
public static class NHoverTipSetProcessCardCyclePreviewPatch
{
    private static void Postfix(NHoverTipSet __instance)
    {
        if (!CardCyclePreviewPatchStorage.Active.TryGetValue(__instance, out CardCyclePreviewState? state))
            return;

        int version = HoverTipResolver.GetVersionKey(state.OriginalTips);

        if (version == state.LastVersion)
            return;

        state.LastVersion = version;

        Rebuild(__instance, state);
    }

    private static void Rebuild(
        NHoverTipSet set,
        CardCyclePreviewState state)
    {
        CardCyclePreviewPatchHelpers.ClearChildren(set._textHoverTipContainer);
        CardCyclePreviewPatchHelpers.ClearChildren(set._cardHoverTipContainer);

        set._textHoverTipContainer.Size = Vector2.Zero;
        set._cardHoverTipContainer.Size = Vector2.Zero;

        set.Init(
            state.Owner,
            CardCyclePreviewPatchHelpers.ResolveAllForCycleSet(state.OriginalTips));

        if (state.Alignment != HoverTipAlignment.None)
        {
            set.SetAlignment(
                state.Owner,
                state.Alignment);
        }
        else
        {
            set.CorrectVerticalOverflow();
            set.CorrectHorizontalOverflow();
        }
    }
}

[HarmonyPatch(typeof(NHoverTipSet), nameof(NHoverTipSet.Remove))]
public static class NHoverTipSetRemoveCardCyclePreviewPatch
{
    private static void Prefix(Control owner)
    {
        if (NHoverTipSet._activeHoverTips.TryGetValue(owner, out NHoverTipSet? set))
            CardCyclePreviewPatchStorage.Active.Remove(set);
    }
}