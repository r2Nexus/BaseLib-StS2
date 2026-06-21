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
    private static void Prefix(
        ref IEnumerable<IHoverTip> hoverTips,
        out List<IHoverTip> __state)
    {
        __state = hoverTips.ToList();
        hoverTips = HoverTipResolver.ResolveAll(__state);
    }

    private static void Postfix(
        Control owner,
        HoverTipAlignment alignment,
        NHoverTipSet? __result,
        List<IHoverTip> __state)
    {
        if (__result == null)
            return;

        if (!HoverTipResolver.HasResolvingTip(__state))
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
        ClearChildren(set._textHoverTipContainer);
        ClearChildren(set._cardHoverTipContainer);

        set._textHoverTipContainer.Size = Vector2.Zero;
        set._cardHoverTipContainer.Size = Vector2.Zero;

        set.Init(
            state.Owner,
            HoverTipResolver.ResolveAll(state.OriginalTips));

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

    private static void ClearChildren(Node parent)
    {
        foreach (Node child in parent.GetChildren().OfType<Node>().ToList())
        {
            parent.RemoveChild(child);
            child.QueueFree();
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