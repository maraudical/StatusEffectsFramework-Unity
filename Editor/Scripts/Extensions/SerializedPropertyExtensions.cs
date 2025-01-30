using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    public static class SerializedPropertyExtensions
    {
        // The following was taken from: https://discussions.unity.com/t/get-the-instance-the-serializedproperty-belongs-to-in-a-custompropertydrawer/66954/2
        public static object GetParent(this SerializedProperty property, UnityEngine.Object targetObject)
        {
            var path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = targetObject;
            var elements = path.Split('.');
            foreach (var element in elements.Take(elements.Length - 1))
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue(obj, elementName, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }
            }
            return obj;
        }

        public static object GetValue(this object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();
            ReflectField:
            var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f == null)
            {
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p == null)
                {
                    if (type.Namespace.Contains(nameof(UnityEngine)))
                        return null;

                    type = type.BaseType;
                    goto ReflectField;
                }
                return p.GetValue(source, null);
            }
            return f.GetValue(source);
        }

        public static void SetValue(this object source, string name, object value)
        {
            if (source == null)
                return;
            var type = source.GetType();
            var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f == null)
            {
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p == null)
                    return;
                p.SetValue(source, value, null);
                return;
            }
            f.SetValue(source, value);
        }

        public static object GetValue(this object source, string name, int index)
        {
            var enumerable = GetValue(source, name) as IEnumerable;
            var enm = enumerable.GetEnumerator();
            while (index-- >= 0)
            {
                enm.MoveNext();
            }
            return enm.Current;
        }

        // Directly taken from Unity.VisualScripting extension
        public static Type GetPropertyType(this SerializedProperty property)
        {
            var type = property.serializedObject.targetObject.GetType();

            var parts = property.propertyPath.Replace(".Array.data[", "[").Split('.');

            foreach (var part in parts)
            {
                type = GetPropertyPartType(part, type);
            }

            return type;

            Type GetPropertyPartType(string propertyPathPart, Type type)
            {
                string fieldName;
                int index;

                if (IsPropertyIndexer(propertyPathPart, out fieldName, out index))
                {
                    var listType = GetSerializedFieldInfo(type, fieldName).FieldType;

                    if (listType.IsArray)
                    {
                        return listType.GetElementType();
                    }
                    else // List<T> is the only other Unity-serializable collection
                    {
                        return listType.GetGenericArguments()[0];
                    }
                }
                else
                {
                    return GetSerializedFieldInfo(type, fieldName).FieldType;
                }
            }

            bool IsPropertyIndexer(string propertyPart, out string fieldName, out int index)
            {
                var regex = new Regex(@"(.+)\[(\d+)\]");
                var match = regex.Match(propertyPart);

                if (match.Success) // Property refers to an array or list
                {
                    fieldName = match.Groups[1].Value;
                    index = int.Parse(match.Groups[2].Value);
                    return true;
                }
                else
                {
                    fieldName = propertyPart;
                    index = -1;
                    return false;
                }
            }

            FieldInfo GetSerializedFieldInfo(Type type, string name)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field == null)
                {
                    throw new MissingMemberException(type.FullName, name);
                }

                return field;
            }
        }
    }
}