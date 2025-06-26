#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PolymorphicEditorTools.Editor
{
    public interface ILazyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDisposable
    {
        void LazyRemove(TKey key);
    }

    public class LazyDictionary<TInput, TKey, TValue> : ILazyDictionary<TKey, TValue>
    {
        private interface IEnumerationState : IEnumerable<IEnumerationState>, IDisposable
        {
            void IDisposable.Dispose() { }
        }

        private sealed class NotStartedEnumerationState : IEnumerationState
        {
            private readonly IEnumerable<TInput> enumerable;
            private readonly Func<TInput, TKey> keyMapper;
            private readonly Func<TInput, TValue> valueMapper;

            public NotStartedEnumerationState(IEnumerable<TInput> enumerable, Func<TInput, TKey> keyMapper, Func<TInput, TValue> valueMapper)
            {
                this.enumerable = enumerable;
                this.keyMapper = keyMapper;
                this.valueMapper = valueMapper;
            }

            public IEnumerationState Start()
            {
                try
                {
                    IEnumerator<TInput> enumerator = enumerable.GetEnumerator();
                    return new EnumeratingEnumerationState(enumerator, keyMapper, valueMapper);
                }
                catch (Exception e)
                {
                    return new ErrorEnumerationState(e);
                }
            }

            public IEnumerator<IEnumerationState> GetEnumerator()
            {
                try
                {
                    IEnumerator<TInput> enumerator = enumerable.GetEnumerator();
                    return new EnumeratingEnumerationState(enumerator, keyMapper, valueMapper).GetEnumerator();
                }
                catch (Exception e)
                {
                    return EnumerableUtils.FromSingleItem<IEnumerationState>(new ErrorEnumerationState(e)).GetEnumerator();
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class EnumeratingEnumerationState : IEnumerationState, IDisposable
        {
            private IEnumerator<TInput>? enumerator;
            private Func<TInput, TKey> keyMapper;
            private readonly Func<TInput, TValue> valueMapper;
            public Dictionary<TKey, TValue> LaterAdditions { get; } = new();
            public Dictionary<TKey, TValue> Dictionary { get; } = new();
            public HashSet<TKey> Removals { get; } = new();
            public bool Disposed { get; private set; } = false;

            public EnumeratingEnumerationState(IEnumerator<TInput> enumerator, Func<TInput, TKey> keyMapper, Func<TInput, TValue> valueMapper)
            {
                this.enumerator = enumerator;
                this.keyMapper = keyMapper;
                this.valueMapper = valueMapper;
            }

            public void LazyRemove(TKey key)
            {
                Removals.Add(key);
                LaterAdditions.Remove(key);
                Dictionary.Remove(key);
            }

            public void LazyOverwrite(TKey key, TValue value)
            {
                Removals.Remove(key);
                LaterAdditions.Remove(key);
                Dictionary[key] = value;
            }

            public (IEnumerationState nextState, (TKey key, TValue value)? keyValue) TryPullNextElement()
            {
                try
                {
                    if (enumerator is not null)
                    {
                        TInput element = enumerator.Current;
                        TKey key = keyMapper(element);
                        (TKey key, TValue value)? keyValue = null;
                        if (!Removals.Contains(key) && !Dictionary.ContainsKey(key))
                        {
                            LaterAdditions.Remove(key);
                            TValue value = valueMapper(element);
                            Dictionary[key] = value;
                            keyValue = (key, value);
                        }
                        if (enumerator.MoveNext())
                        {
                            return (this, keyValue);
                        }
                        else
                        {
                            Dispose();
                        }
                    }
                    if (LaterAdditions.Count > 0)
                    {
                        TKey key = LaterAdditions.First().Key;
                        TValue value = LaterAdditions[key];
                        Asserts.IsTrue(LaterAdditions.Remove(key));
                        Dictionary[key] = value;
                        return (this, (key, value));
                    }
                    else
                    {
                        return (new CompletedEnumerationState(Dictionary), default);
                    }
                }
                catch (Exception e)
                {
                    Dispose();
                    return (new ErrorEnumerationState(e), null);
                }
            }


            public void Dispose()
            {
                if (!Disposed)
                {
                    try
                    {
                        enumerator?.Dispose();
                        Removals.Clear();
                    }
                    finally
                    {
                        enumerator = null;
                        Disposed = true;
                    }
                }
            }


            public IEnumerator<IEnumerationState> GetEnumerator()
            {
                IEnumerationState currentState = this;
                while (currentState is EnumeratingEnumerationState enumerating)
                {
                    (currentState, _) = enumerating.TryPullNextElement();
                    yield return currentState;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class CompletedEnumerationState : IEnumerationState
        {
            public Dictionary<TKey, TValue> Dictionary { get; } = new();

            public CompletedEnumerationState(Dictionary<TKey, TValue> values)
            {
                Dictionary = values;
            }

            public IEnumerator<IEnumerationState> GetEnumerator()
            {
                return Enumerable.Empty<IEnumerationState>().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class ErrorEnumerationState : IEnumerationState
        {
            public Exception Exception { get; }

            public ErrorEnumerationState(Exception exception)
            {
                Exception = exception;
            }

            public IEnumerator<IEnumerationState> GetEnumerator()
            {
                throw Exception;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class DisposedEnumerationState : IEnumerationState
        {
            public IEnumerator<IEnumerationState> GetEnumerator()
            {
                throw new ObjectDisposedException(nameof(LazyDictionary<TInput, TKey, TValue>));
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private IEnumerationState state;

        public LazyDictionary(IEnumerable<TInput> enumerable, Func<TInput, TKey> keyMapper, Func<TInput, TValue> valueMapper)
        {
            state = new NotStartedEnumerationState(enumerable, keyMapper, valueMapper);
        }

        public void LazyRemove(TKey key)
        {
            EnsurePreconditions();
            if (state is CompletedEnumerationState completed)
            {
                _ = completed.Dictionary.Remove(key);
            }
            else if (state is EnumeratingEnumerationState enumerating)
            {
                enumerating.LazyRemove(key);
            }
            throw StateFailsPostCondition();
        }


        public TValue this[TKey key]
        {
            get
            {
                EnsurePreconditions();
                if (state is CompletedEnumerationState completed)
                {
                    return completed.Dictionary[key];
                }
                if (state is EnumeratingEnumerationState enumerating)
                {
                    if (enumerating.Removals.Contains(key))
                    {
                        return default!;
                    }
                    else if (enumerating.Dictionary.TryGetValue(key, out TValue? value))
                    {
                        return value;
                    }
                    else
                    {
                        foreach ((TKey thisKey, TValue thisValue) in GetEnumerableFromEnumeratingState(enumerating))
                        {
                            if (Equals(thisKey, key))
                            {
                                return thisValue;
                            }
                        }
                    }
                }
                throw StateFailsPostCondition();
            }
            set
            {
                EnsurePreconditions();
                if (state is CompletedEnumerationState completed)
                {
                    completed.Dictionary[key] = value!;
                }
                if (state is EnumeratingEnumerationState enumerating)
                {
                    enumerating.LazyOverwrite(key, value!);
                }
                throw StateFailsPostCondition();
            }
        }

        public ICollection<TKey> Keys =>
            Force().Dictionary.Keys;

        public ICollection<TValue> Values =>
            Force().Dictionary.Values;

        public int Count => Force().Dictionary.Count;

        public bool IsReadOnly => false;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        public void Add(TKey key, TValue value) => Force().Dictionary.Add(key, value);

        public void Add(KeyValuePair<TKey, TValue> item) =>
           ((ICollection<KeyValuePair<TKey, TValue>>)Force().Dictionary).Add(item);

        public void Clear()
        {
            state.Dispose();
            state = new CompletedEnumerationState(new Dictionary<TKey, TValue>());
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) =>
            ((ICollection<KeyValuePair<TKey, TValue>>)Force().Dictionary).Contains(item);

        public bool ContainsKey(TKey key) =>
            Force().Dictionary.ContainsKey(key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
            ((ICollection<KeyValuePair<TKey, TValue>>)Force().Dictionary).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            EnsurePreconditions();
            if (state is CompletedEnumerationState completed)
            {
                return completed.Dictionary.GetEnumerator();
            }
            else if (state is EnumeratingEnumerationState enumerating)
            {
                return GetEnumerableFromEnumeratingState(enumerating).GetEnumerator();
            }
            throw StateFailsPostCondition();
        }

        private IEnumerable<KeyValuePair<TKey, TValue>> GetEnumerableFromEnumeratingState(EnumeratingEnumerationState enumerating)
        {
            while (true)
            {
                (IEnumerationState nextState, (TKey key, TValue value)? maybeKeyValue) = enumerating.TryPullNextElement();
                state = nextState;
                ThrowIfErrorOrDisposedState();
                if (maybeKeyValue is { } keyValue)
                {
                    yield return new KeyValuePair<TKey, TValue>(keyValue.key, keyValue.value);
                }
                if (state is not EnumeratingEnumerationState nextEnumerating)
                {
                    yield break;
                }
                enumerating = nextEnumerating;
            }
        }

        public bool Remove(TKey key) =>
            Force().Dictionary.Remove(key);

#pragma warning disable CS8767 // out value needs to be nullable (TValue?) for this contract to make sense
        public bool TryGetValue(TKey key, out TValue? value)
        {
            EnsurePreconditions();
            if (state is CompletedEnumerationState completed)
            {
                return completed.Dictionary.TryGetValue(key, out value);
            }
            else if (state is EnumeratingEnumerationState enumerating)
            {
                if (enumerating.Removals.Contains(key))
                {
                    value = default;
                    return false;
                }
                else if (enumerating.Dictionary.TryGetValue(key, out value))
                {
                    return true;
                }
                else
                {
                    foreach ((TKey thisKey, TValue thisValue) in GetEnumerableFromEnumeratingState(enumerating))
                    {
                        if (Equals(thisKey, key))
                        {
                            value = thisValue;
                            return true;
                        }
                    }
                    return false;
                }
            }
            throw Asserts.Fail($"{nameof(state)} is {nameof(EnumeratingEnumerationState)} or ${nameof(CompletedEnumerationState)}");
        }
#pragma warning restore CS8767

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            try
            {
                state.Dispose();
            }
            finally
            {
                state = new DisposedEnumerationState();
            }
        }

        private void StartEnumerationIfNecessary()
        {
            if (state is NotStartedEnumerationState notStarted)
            {
                state = notStarted.Start();
                ThrowIfErrorOrDisposedState();
            }
        }

        private void ThrowIfErrorOrDisposedState()
        {
            if (state is ErrorEnumerationState error)
            {
                throw error.Exception;
            }
            if (state is DisposedEnumerationState)
            {
                throw new ObjectDisposedException(nameof(LazyDictionary<TInput, TKey, TValue>));
            }
        }

        private void EnsurePreconditions()
        {
            ThrowIfErrorOrDisposedState();
            StartEnumerationIfNecessary();
        }

        public Exception StateFailsPostCondition()
        {
            throw Asserts.Fail($"{nameof(state)} is {nameof(EnumeratingEnumerationState)} or ${nameof(CompletedEnumerationState)}");
        }

        private CompletedEnumerationState Force()
        {
            EnsurePreconditions();
            IEnumerationState initialState = state;
            foreach (IEnumerationState newState in initialState)
            {
                state = newState;
                ThrowIfErrorOrDisposedState();
            }
            return Asserts.IsType<IEnumerationState, CompletedEnumerationState>(state);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) =>
            ((ICollection<KeyValuePair<TKey, TValue>>)Force().Dictionary).Remove(item);
    }
}
