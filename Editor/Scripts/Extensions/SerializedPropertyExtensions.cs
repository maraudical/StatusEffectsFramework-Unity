using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;

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
                enm.MoveNext();
            return enm.Current;
        }
    }
}