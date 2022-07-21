/*

Copyright (c) 2020 Jean-Paul Mikkers

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

*/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Linq;

namespace DHCPServerApp
{
    public static class DeepCopier
    {
        public static T Copy<T>(T original)
        {
            return (T)new DeepCopyContext().InternalCopy(original, true);
        }

        private class DeepCopyContext
        {
            private static readonly Func<object, object> s_cloneMethod;
            private readonly Dictionary<Object, Object> _visited;
            private readonly Dictionary<Type, FieldInfo[]> _nonShallowFieldCache;

            static DeepCopyContext()
            {
                MethodInfo cloneMethod = typeof(Object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
                var p1 = Expression.Parameter(typeof(object));
                var body = Expression.Call(p1, cloneMethod);
                s_cloneMethod = Expression.Lambda<Func<object, object>>(body, p1).Compile();
                //Console.WriteLine("typeof(object) contains {0} nonshallow fields", NonShallowFields(typeof(object)).Count());
            }

            public DeepCopyContext()
            {
                _visited = new Dictionary<object, object>(new ReferenceEqualityComparer());
                _nonShallowFieldCache = new Dictionary<Type, FieldInfo[]>();
            }

            private static bool IsPrimitiveOrImmutable(Type type)
            {
                if (type.IsPrimitive)
                {
                    return true;
                }
                else if (type.IsValueType)
                {
                    if (type.IsEnum) return true;
                    if (type == typeof(Decimal)) return true;
                    if (type == typeof(DateTime)) return true;
                }
                else
                {
                    if (type == typeof(String)) return true;
                }
                return false;
            }

            public Object InternalCopy(Object originalObject, bool includeInObjectGraph)
            {
                if (originalObject == null) return null;
                var typeToReflect = originalObject.GetType();
                if (IsPrimitiveOrImmutable(typeToReflect)) return originalObject;

                if (typeof(XElement).IsAssignableFrom(typeToReflect)) return new XElement(originalObject as XElement);
                if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;

                if (includeInObjectGraph)
                {
                    object result;
                    if (_visited.TryGetValue(originalObject, out result)) return result;
                }

                var cloneObject = s_cloneMethod(originalObject);

                if (includeInObjectGraph)
                {
                    _visited.Add(originalObject, cloneObject);
                }

                if (typeToReflect.IsArray)
                {
                    var arrayElementType = typeToReflect.GetElementType();

                    if (IsPrimitiveOrImmutable(arrayElementType))
                    {
                        // for an array of primitives, do nothing. The shallow clone is enough.
                    }
                    else if (arrayElementType.IsValueType)
                    {
                        // if its an array of structs, there's no need to check and add the individual elements to 'visited', because in .NET it's impossible to create
                        // references to individual array elements.
                        ReplaceArrayElements((Array)cloneObject, x => InternalCopy(x, false));
                    }
                    else
                    {
                        // it's an array of ref types
                        ReplaceArrayElements((Array)cloneObject, x => InternalCopy(x, true));
                    }
                }
                else
                {
                    foreach (var fieldInfo in CachedNonShallowFields(typeToReflect))
                    {
                        var originalFieldValue = fieldInfo.GetValue(originalObject);
                        // a valuetype field can never have a reference pointing to it, so don't check the object graph in that case
                        var clonedFieldValue = InternalCopy(originalFieldValue, !fieldInfo.FieldType.IsValueType);
                        fieldInfo.SetValue(cloneObject, clonedFieldValue);
                    }
                }

                return cloneObject;
            }

            private static void ReplaceArrayElements(Array array, Func<object, object> func, int dimension, int[] counts, int[] indices)
            {
                int len = counts[dimension];

                if (dimension < (counts.Length - 1))
                {
                    // not the final dimension, loop the range, and recursively handle one dimension higher
                    for (int t = 0; t < len; t++)
                    {
                        indices[dimension] = t;
                        ReplaceArrayElements(array, func, dimension + 1, counts, indices);
                    }
                }
                else
                {
                    // we've reached the final dimension where the elements are closest together in memory. Do a final loop.
                    for (int t = 0; t < len; t++)
                    {
                        indices[dimension] = t;
                        array.SetValue(func(array.GetValue(indices)), indices);
                    }
                }
            }

            private static void ReplaceArrayElements(Array array, Func<object, object> func)
            {
                if (array.Rank == 1)
                {
                    // do a fast loop for the common case, a one dimensional array
                    int len = array.GetLength(0);
                    for (int t = 0; t < len; t++)
                    {
                        array.SetValue(func(array.GetValue(t)), t);
                    }
                }
                else
                {
                    // multidimensional array: recursively loop through all dimensions, starting with dimension zero.
                    var counts = Enumerable.Range(0, array.Rank).Select(array.GetLength).ToArray();
                    var indices = new int[array.Rank];
                    ReplaceArrayElements(array, func, 0, counts, indices);
                }
            }

            private FieldInfo[] CachedNonShallowFields(Type typeToReflect)
            {
                FieldInfo[] result;

                if (!_nonShallowFieldCache.TryGetValue(typeToReflect, out result))
                {
                    result = NonShallowFields(typeToReflect).ToArray();
                    _nonShallowFieldCache[typeToReflect] = result;
                }

                return result;
            }

            /// <summary>
            /// From the given type hierarchy (i.e. including all base types), return all fields that should be deep-copied
            /// </summary>
            /// <param name="typeToReflect"></param>
            /// <returns></returns>
            private static IEnumerable<FieldInfo> NonShallowFields(Type typeToReflect)
            {
                while (typeToReflect != typeof(object))
                {
                    foreach (var fieldInfo in typeToReflect.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                    {
                        if (IsPrimitiveOrImmutable(fieldInfo.FieldType)) continue; // this is 5% faster than a where clause..
                        yield return fieldInfo;
                    }
                    typeToReflect = typeToReflect.BaseType;
                }
            }
        }
    }

    public class ReferenceEqualityComparer : EqualityComparer<Object>
    {
        public override bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public override int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            // The RuntimeHelpers.GetHashCode method always calls the Object.GetHashCode method non-virtually, 
            // even if the object's type has overridden the Object.GetHashCode method.
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
