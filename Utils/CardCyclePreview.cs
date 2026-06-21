using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

/// <summary>
/// Utility methods for creating hover tips that cycle between several card previews.
/// </summary>
public static class CardCyclePreview
{
    public static IHoverTip FromCards(
        params CardModel[] cards)
    {
        return FromCards(cards.AsEnumerable());
    }

    public static IHoverTip FromCards(
        IEnumerable<CardModel> cards,
        CardCyclePreviewOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(cards);

        List<CardModel> resolvedCards = cards
            .Where(card => card != null)
            .ToList();

        return FromResolver(
            () => resolvedCards,
            options);
    }

    public static IHoverTip FromResolver(
        Func<IEnumerable<CardModel>> resolver,
        CardCyclePreviewOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        return new CardCyclePreviewHoverTip(
            resolver,
            options);
    }

    public static IHoverTip FromPool(
        CardModel sourceCard,
        Func<CardModel, bool> predicate,
        CardCyclePreviewOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(sourceCard);
        ArgumentNullException.ThrowIfNull(predicate);

        return FromResolver(
            () =>
            {
                var owner = sourceCard.Owner;

                if (owner == null)
                    return [];

                return owner.Character.CardPool
                    .GetUnlockedCards(
                        owner.UnlockState,
                        owner.RunState.CardMultiplayerConstraint)
                    .Where(predicate);
            },
            options);
    }

    public static IHoverTip FromTag(
        CardModel sourceCard,
        CardTag tag,
        CardCyclePreviewOptions? options = null)
    {
        return FromPool(
            sourceCard,
            card => card.Tags.Contains(tag),
            options);
    }
}