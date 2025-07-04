#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Polymorphism4Unity.Safety;
using UnityEngine;

namespace Polymorphism4Unity.KindSets
{

    [Serializable]
    public class KindSet<TBaseKind> : IEnumerable<TBaseKind>, ICollection<TBaseKind>, ISerializationCallbackReceiver
    {
        [SerializeReference]
        private TBaseKind[] backingValues = new TBaseKind[0];
        private readonly OrderedDictionary values = new();
        public int Count => values.Count;
        public bool IsReadOnly => false;
        public IEnumerable<Type> Types => values.Keys.Cast<Type>();
        public IEnumerable<TBaseKind> Values => values.Values.Cast<TBaseKind>();

        public TBaseKind this[Type t]
        {
            get
            {
                return (TBaseKind)values[t];
            }
            set
            {
                if (value is not { } notNullValue)
                {
                    throw new ArgumentNullException();
                }
                Type valueKind = notNullValue.GetType();
                values[valueKind] = notNullValue;
            }
        }

        public TBaseKind this[int i] =>
            (TBaseKind)values[i];

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            values.Clear();
            for (int i = 0; i < backingValues.Length; ++i)
            {
                TBaseKind item = Asserts.IsNotNull(backingValues[i])!;
                Type itemKind = item.GetType();
                Asserts.IsFalse(values.Contains(itemKind));
                values[itemKind] = item;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            backingValues = new TBaseKind[values.Count];
            CopyTo(backingValues, 0);
        }

        public bool Add(TBaseKind item)
        {
            if (item is null)
            {
                return false;
            }
            Type itemKind = item.GetType();
            if (values.Contains(itemKind))
            {
                return false;
            }
            values[itemKind] = item;
            return true;
        }

        public bool Remove(Type kind)
        {
            if (!values.Contains(kind))
            {
                return false;
            }
            values.Remove(kind);
            return true;
        }

        public bool Remove<TSubkind>()
            where TSubkind : TBaseKind
        {
            return Remove(typeof(TSubkind));
        }

        public IEnumerator<TBaseKind> GetEnumerator()
        {
            return values.Values.Cast<TBaseKind>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Clear()
        {
            values.Clear();
            backingValues = new TBaseKind[0];
        }

        public bool Contains(Type subkind)
        {
            return values.Contains(subkind);
        }

        public void CopyTo(TBaseKind[] destination, int destinationStartIndex)
        {
            int sourceLength = values.Count;
            int destinationLength = destination.Length;
            if (sourceLength > destinationLength - destinationStartIndex)
            {
                throw new ArgumentException($"Length of this collection exceeds  ({nameof(destination)}.Count - {nameof(destinationStartIndex)}) meaning the collection would not be fully copied");
            }
            for (int destIndex = destinationStartIndex, sourceIndex = 0; destIndex < destinationLength; ++destIndex, ++sourceIndex)
            {
                TBaseKind item = (TBaseKind)values[sourceIndex];
                destination[destIndex] = item;
            }
        }

        void ICollection<TBaseKind>.Add(TBaseKind item)
        {
            if (!Add(item))
            {
                throw new ArgumentException($"An item with the same kind already exists in this {nameof(KindSet<TBaseKind>)}");
            }
        }

        public bool Contains(TBaseKind item)
        {
            if (item is null)
            {
                return false;
            }
            Type itemKind = item.GetType();
            return Equals((TBaseKind)values[itemKind], item);
        }

        public bool Remove(TBaseKind item)
        {
            if (Contains(item) && item is { })
            {
                values.Remove(item.GetType());
                return true;
            }
            return false;
        }
    }
}