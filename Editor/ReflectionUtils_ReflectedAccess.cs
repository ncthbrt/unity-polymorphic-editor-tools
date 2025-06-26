#nullable enable
using System;
using System.Reflection;

namespace PolymorphicEditorTools.Editor
{
    public partial class ReflectionUtils
    {
        public static void ResetReflectedAccess()
        {
        }

        public const BindingFlags NonPublicBindingFlag = BindingFlags.NonPublic;
        public const BindingFlags IgnoreCaseBindingFlag = BindingFlags.IgnoreCase;
        public const BindingFlags FlattenHierarchyBindingFlag = BindingFlags.FlattenHierarchy;
        public const BindingFlags DefaultBindingFlag = BindingFlags.Default;
        public const BindingFlags DeclaredOnlyBindingFlag = BindingFlags.Instance;
        public const BindingFlags InstanceBindingFlag = BindingFlags.Instance;
        public const BindingFlags StaticBindingFlag = BindingFlags.Static;
        public const BindingFlags PublicBindingFlag = BindingFlags.Public;
        public const BindingFlags InvokeMethodBindingFlag = BindingFlags.InvokeMethod;
        public const BindingFlags CreateInstanceBindingFlag = BindingFlags.CreateInstance;
        public const BindingFlags GetFieldBindingFlag = BindingFlags.GetField;
        public const BindingFlags SetFieldBindingFlag = BindingFlags.SetField;
        public const BindingFlags GetPropertyBindingFlag = BindingFlags.GetProperty;
        public const BindingFlags SetPropertyBindingFlag = BindingFlags.SetProperty;
        public const BindingFlags PutDispPropertyBindingFlag = BindingFlags.PutDispProperty;
        public const BindingFlags ExactBindingBindingFlag = BindingFlags.ExactBinding;
        public const BindingFlags SuppressChangeTypeBindingFlag = BindingFlags.SuppressChangeType;
        public const BindingFlags OptionalParamBindingBindingFlag = BindingFlags.OptionalParamBinding;
        public const BindingFlags IgnoreReturnBindingFlag = BindingFlags.IgnoreReturn;
        public const BindingFlags DoNotWrapExceptionsBindingFlag = BindingFlags.DoNotWrapExceptions;

        public static BindingFlags AndIgnoreCase(this BindingFlags flag)
        {
            return flag | BindingFlags.IgnoreCase;
        }

        public static BindingFlags AndNonPublic(this BindingFlags flag)
        {
            return flag | BindingFlags.NonPublic;
        }


        public static BindingFlags AndFlattenHierarchy(this BindingFlags flag)
        {
            return flag | BindingFlags.FlattenHierarchy;
        }

        public static BindingFlags AndDeclaredOnly(this BindingFlags flag)
        {
            return flag | BindingFlags.DeclaredOnly;
        }
        public static BindingFlags AndInstance(this BindingFlags flag)
        {
            return flag | BindingFlags.Instance;
        }
        public static BindingFlags AndStatic(this BindingFlags flag)
        {
            return flag | BindingFlags.Static;
        }
        public static BindingFlags AndPublic(this BindingFlags flag)
        {
            return flag | BindingFlags.Public;
        }
        public static BindingFlags AndInvokeMethod(this BindingFlags flag)
        {
            return flag | BindingFlags.InvokeMethod;
        }
        public static BindingFlags AndCreateInstance(this BindingFlags flag)
        {
            return flag | BindingFlags.CreateInstance;
        }
        public static BindingFlags AndGetField(this BindingFlags flag)
        {
            return flag | BindingFlags.GetField;
        }
        public static BindingFlags AndSetField(this BindingFlags flag)
        {
            return flag | BindingFlags.SetField;
        }
        public static BindingFlags AndGetProperty(this BindingFlags flag)
        {
            return flag | BindingFlags.GetProperty;
        }
        public static BindingFlags AndSetProperty(this BindingFlags flag)
        {
            return flag | BindingFlags.SetProperty;
        }
        public static BindingFlags AndPutDispProperty(this BindingFlags flag)
        {
            return flag | BindingFlags.PutDispProperty;
        }
        public static BindingFlags AndExactBinding(this BindingFlags flag)
        {
            return flag | BindingFlags.ExactBinding;
        }
        public static BindingFlags AndSuppressChangeType(this BindingFlags flag)
        {
            return flag | BindingFlags.SuppressChangeType;
        }
        public static BindingFlags AndOptionalParamBinding(this BindingFlags flag)
        {
            return flag | BindingFlags.OptionalParamBinding;
        }
        public static BindingFlags AndIgnoreReturn(this BindingFlags flag)
        {
            return flag | BindingFlags.IgnoreReturn;
        }
        public static BindingFlags AndDoNotWrapExceptions(this BindingFlags flag)
        {
            return flag | BindingFlags.DoNotWrapExceptions;
        }


        private static bool TryGetNonPublicMemberValue<TExpectedResult>(object obj, Type currentType, string memberName, out TExpectedResult? maybeValue, BindingFlags additionalFlags = DefaultBindingFlag)
        {
            foreach (MemberInfo memberInfo in currentType.GetMember(memberName, MemberTypes.Property | MemberTypes.Field, NonPublicBindingFlag.AndDeclaredOnly().AndInstance() | additionalFlags))
            {
                if (memberInfo is PropertyInfo propertyInfo && propertyInfo.PropertyType.Is<TExpectedResult>())
                {
                    switch (propertyInfo.GetValue(obj))
                    {
                        case TExpectedResult expectedResult:
                            maybeValue = expectedResult;
                            return true;
                        case null:
                            maybeValue = default;
                            return true;
                        default:
                            continue;
                    }
                }
                else if (memberInfo is FieldInfo fieldInfo && fieldInfo.FieldType.Is<TExpectedResult>())
                {
                    switch (fieldInfo.GetValue(obj))
                    {
                        case TExpectedResult expectedResult:
                            maybeValue = expectedResult;
                            return true;
                        case null:
                            maybeValue = default;
                            return true;
                        default:
                            continue;
                    }
                }
            }
            maybeValue = default;
            return false;
        }

        public static bool TryGetNonPublicMemberValue<TExpectedResult>(this object obj, string memberName, out TExpectedResult? maybeValue, BindingFlags additionalFlags = DefaultBindingFlag)
        {
            Type objectType = obj.GetType();
            return TryGetNonPublicMemberValue(obj, objectType, memberName, out maybeValue, additionalFlags);
        }

        public static bool TryGetNonPublicMemberValueRecursive<TExpectedResult>(this object obj, string memberName, out TExpectedResult? maybeValue, BindingFlags additionalFlags = DefaultBindingFlag)
        {
            Type? currentType = obj.GetType();
            while (currentType is not null)
            {
                if (TryGetNonPublicMemberValue(obj, currentType, memberName, out maybeValue, additionalFlags))
                {
                    return true;
                }
                currentType = currentType.BaseType;
            }
            maybeValue = default;
            return false;
        }
    }
}