using System.Collections.Immutable;

namespace IntegrationPlanner.Core.Editing.Commands;

internal static class ImmutableListExtensions
{
    public static ImmutableList<T> AddWithUniqueId<T, TId>(this ImmutableList<T> items, T item, Func<T, TId> idOf)
        where TId : IEquatable<TId>
    {
        TId id = idOf(item);
        return items.Exists(existing => idOf(existing).Equals(id))
            ? throw new ArgumentException($"{typeof(T).Name} '{id}' already exists.", nameof(item))
            : items.Add(item);
    }

    public static ImmutableList<T> ReplaceById<T, TId>(this ImmutableList<T> items, T item, Func<T, TId> idOf)
        where TId : IEquatable<TId>
    {
        TId id = idOf(item);
        int index = items.FindIndex(existing => idOf(existing).Equals(id));
        return index >= 0
            ? items.SetItem(index, item)
            : throw new KeyNotFoundException($"{typeof(T).Name} '{id}' does not exist.");
    }

    public static void EnsureContains<T, TId>(this ImmutableList<T> items, TId id, Func<T, TId> idOf)
        where TId : IEquatable<TId>
    {
        if (items.Exists(existing => idOf(existing).Equals(id)))
        {
            return;
        }

        throw new KeyNotFoundException($"{typeof(T).Name} '{id}' does not exist.");
    }
}
