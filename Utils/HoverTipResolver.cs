using MegaCrit.Sts2.Core.HoverTips;

namespace BaseLib.Utils;

public static class HoverTipResolver
{
    public static IHoverTip? Resolve(IHoverTip tip)
    {
        return tip is IResolvingHoverTip resolving
            ? resolving.ResolveHoverTip()
            : tip;
    }

    public static List<IHoverTip> ResolveAll(IEnumerable<IHoverTip> tips)
    {
        List<IHoverTip> result = [];

        foreach (IHoverTip tip in tips)
        {
            IHoverTip? resolved = Resolve(tip);

            if (resolved != null)
                result.Add(resolved);
        }

        return result;
    }

    public static bool HasResolvingTip(IEnumerable<IHoverTip> tips)
    {
        return tips.Any(tip => tip is IResolvingHoverTip);
    }

    public static int GetVersionKey(IEnumerable<IHoverTip> tips)
    {
        unchecked
        {
            int hash = 17;

            foreach (IHoverTip tip in tips)
            {
                if (tip is IResolvingHoverTip resolving)
                    hash = hash * 31 + resolving.ResolveVersion;
            }

            return hash;
        }
    }
}