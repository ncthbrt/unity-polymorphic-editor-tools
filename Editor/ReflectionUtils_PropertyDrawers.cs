#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace PolymorphicEditorTools.Editor
{
    public static partial class ReflectionUtils
    {
        private class CustomPropertyDrawerData
        {
            private const string typeFieldName = "m_Type";
            private const string useForChildrenFieldName = "m_UseForChildren";
            public Type TargetType { get; private set; }
            public Type PropertyDrawerType { get; private set; }
            public bool UseForChildren { get; private set; }

            public CustomPropertyDrawerData(Type targetType, Type propertyDrawerType, bool useForChildren)
            {
                TargetType = targetType;
                UseForChildren = useForChildren;
                PropertyDrawerType = propertyDrawerType;
            }

            public static IEnumerable<CustomPropertyDrawerData> FromPropertyDrawerType(Type propertyDrawerType)
            {
                foreach (CustomPropertyDrawer attribute in propertyDrawerType.GetCustomAttributes<CustomPropertyDrawer>())
                {
                    if (attribute.TryGetNonPublicMemberValue(typeFieldName, out Type? targetType) && attribute.TryGetNonPublicMemberValue(useForChildrenFieldName, out bool useForChildren))
                    {
                        yield return new CustomPropertyDrawerData(Asserts.IsNotNull(targetType), propertyDrawerType, useForChildren);
                    }
                }
            }
        }

        private static ICachedEnumerable<Type> propertyDrawersTypes = GetPropertyDrawerTypes();
        private static ICachedEnumerable<Type> GetPropertyDrawerTypes() =>
            TypeCache.GetTypesDerivedFrom<PropertyDrawer>().Where(x => x.IsConcreteType() && x.HasDefaultPublicConstructor()).Cached();

        private static IReadOnlyDictionary<Type, CustomPropertyDrawerData> propertyDrawerMappings = GetPropertyDrawerMappings();

        private static IReadOnlyDictionary<Type, CustomPropertyDrawerData> GetPropertyDrawerMappings() =>
            propertyDrawersTypes.SelectMany(CustomPropertyDrawerData.FromPropertyDrawerType).ToLazyDictionary(x => x.TargetType, x => x);

        private static readonly Dictionary<string, Dictionary<Type, PropertyDrawer>> propertyDrawerInstances = new();

        // Maps the property tar to a concrete instance of the appropriate property drawer
        private static readonly Dictionary<Type, PropertyDrawer> propertyTypeMappings = new();

        public static PropertyDrawer? ResolvePropertyDrawer(FieldInfo fieldInfo)
        {
            return null;
        }

        public static PropertyDrawer? ResolvePropertyDrawer(Type type)
        {
            return null;
        }

        private static void ResetPropertyDrawers()
        {
            propertyDrawersTypes = GetPropertyDrawerTypes();
            propertyDrawerInstances.Clear();
            propertyDrawerMappings = GetPropertyDrawerMappings();
            propertyTypeMappings.Clear();
        }
    }
}