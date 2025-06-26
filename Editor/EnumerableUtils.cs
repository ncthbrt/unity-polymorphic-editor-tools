using System;
using System.Collections.Generic;

namespace PolymorphicEditorTools.Editor
{

    public static class EnumerableUtils
    {
        public static IEnumerable<T> FromSingleItem<T>(T item)
        {
            yield return item;
        }

        public static LazyDictionary<TInput, TKey, TValue> ToLazyDictionary<TInput, TKey, TValue>(this IEnumerable<TInput> enumerable, Func<TInput, TKey> keyMapper, Func<TInput, TValue> valueMapper) =>
            new LazyDictionary<TInput, TKey, TValue>(enumerable, keyMapper, valueMapper);

        public static ICachedEnumerable<TElement> Cached<TElement>(this IEnumerable<TElement> enumerable) => Create(enumerable);

        public static ICachedEnumerable<TElement> Create<TElement>(IEnumerable<TElement> enumerable)
        {
            ICachedEnumerable<TElement> inner = new NotStartedCachedEnumerable<TElement>(enumerable);
            return new WrappedCachedEnumerable<TElement>(inner);
        }
    }
}