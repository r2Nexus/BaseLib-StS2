using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

internal static class CardCyclePreviewCardRegistry
{
    private static readonly Dictionary<Type, IReadOnlyList<CardModel>> CardsByPool = [];

    public static IReadOnlyList<CardModel> GetCardsFromPool<TPool>()
        where TPool : CardPoolModel
    {
        return GetCardsFromPool(typeof(TPool));
    }

    public static IReadOnlyList<CardModel> GetCardsFromPool(Type poolType)
    {
        ArgumentNullException.ThrowIfNull(poolType);

        if (!typeof(CardPoolModel).IsAssignableFrom(poolType))
            throw new ArgumentException(
                $"{poolType.FullName} is not a {nameof(CardPoolModel)}.",
                nameof(poolType));

        if (CardsByPool.TryGetValue(poolType, out IReadOnlyList<CardModel>? cached))
            return cached;

        List<CardModel> cards = [];

        foreach (Type type in CustomContentDictionary.RegisteredTypes)
        {
            if (type.IsAbstract)
                continue;

            if (!typeof(CardModel).IsAssignableFrom(type))
                continue;

            PoolAttribute? poolAttribute = type.GetCustomAttribute<PoolAttribute>(inherit: true);

            if (poolAttribute == null)
                continue;

            if (!poolType.IsAssignableFrom(poolAttribute.PoolType))
                continue;

            if (ModelDb.GetById<CardModel>(ModelDb.GetId(type)) is { } card)
                cards.Add(card);
        }

        cards = cards
            .DistinctBy(card => card.GetType())
            .OrderBy(card => card.Id.ToString())
            .ToList();

        CardsByPool[poolType] = cards;
        return cards;
    }
}