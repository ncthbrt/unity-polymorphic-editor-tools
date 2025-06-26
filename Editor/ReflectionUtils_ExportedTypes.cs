#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PolymorphicEditorTools.Editor
{
    public static partial class ReflectionUtils
    {
        private static IEnumerable<Type> GetExportedTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.IsDynamic)
                {
                    Type[] exportedTypes;
                    try
                    {
                        exportedTypes = assembly.GetExportedTypes() ?? Type.EmptyTypes;
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        exportedTypes = e.Types;
                    }
                    catch (Exception e)
                    {
                        exportedTypes = Array.Empty<Type>();
                        LoggerProvider.LogException(e);
                    }
                    foreach (Type t in exportedTypes)
                    {
                        yield return t;
                    }
                }
            }
        }

        private static ICachedEnumerable<Type> exportedTypes = GetExportedTypes().Cached();
        public static IEnumerable<Type> Types => exportedTypes;

        private static void ResetExportedTypes()
        {
            exportedTypes = GetExportedTypes().Cached();
        }
    }

}