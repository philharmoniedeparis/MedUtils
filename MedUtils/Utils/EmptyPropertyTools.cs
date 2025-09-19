using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MedUtils.Utils;
public static class EmptyPropertyTools
{
    public static bool IsEmpty(object? value, Type type)
    {
        if (value is null) return true;

        // strings
        if (type == typeof(string))
            return string.IsNullOrEmpty((string)value); // use IsNullOrWhiteSpace if you prefer

        // Collections with Count
        if (value is ICollection nonGenericColl)
            return nonGenericColl.Count == 0;

        var countProp = type.GetProperty("Count");
        if (countProp != null && countProp.PropertyType == typeof(int) && type != typeof(string))
        {
            var count = (int)countProp.GetValue(value)!;
            return count == 0;
        }

        // IEnumerable fallback
        if (value is IEnumerable en)
        {
            var enumerator = en.GetEnumerator();
            return !enumerator.MoveNext();
        }

        // Nullable<T>
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            var hasValueProp = type.GetProperty("HasValue");
            if (hasValueProp != null)
            {
                bool hasValue = (bool)hasValueProp.GetValue(value)!;
                return !hasValue;
            }
        }

        return false;
    }

    /// <summary>
    /// Trim leading empty "" elements for string collections/arrays.
    /// Returns true if it modified the value (including setting to null).
    /// </summary>
    private static bool TrimLeadingEmptyStringForStringCollections<T>(T obj, PropertyInfo p, object? val, out object? newValue)
    {
        newValue = val;

        // List<string>
        if (val is List<string> list)
        {
            // remove first (or multiple leading) empty entries
            while (list.Count > 0 && string.IsNullOrEmpty(list[0]))
                list.RemoveAt(0);

            if (list.Count == 0)
            {
                newValue = null;
                p.SetValue(obj, null);
                return true;
            }

            // modified in place; no need to SetValue again
            return true;
        }

        // string[]
        if (val is string[] arr)
        {
            var trimmed = arr.SkipWhile(s => string.IsNullOrEmpty(s)).ToArray();
            if (trimmed.Length == 0)
            {
                newValue = null;
                p.SetValue(obj, null);
                return true;
            }
            if (!ReferenceEquals(trimmed, arr))
            {
                // same type (string[]) – assign back
                p.SetValue(obj, trimmed);
                newValue = trimmed;
                return true;
            }
            return false;
        }

        // Other IEnumerable<string> (e.g., IReadOnlyList<string>, IEnumerable<string>)
        if (val is IEnumerable<string> seq)
        {
            var tmp = seq.ToList();
            int originalCount = tmp.Count;

            while (tmp.Count > 0 && string.IsNullOrEmpty(tmp[0]))
                tmp.RemoveAt(0);

            if (tmp.Count == 0)
            {
                newValue = null;
                p.SetValue(obj, null);
                return true;
            }

            if (tmp.Count != originalCount && p.CanWrite)
            {
                // Assign a List<string> back if compatible
                if (p.PropertyType.IsAssignableFrom(typeof(List<string>)))
                {
                    p.SetValue(obj, tmp);
                    newValue = tmp;
                    return true;
                }
                // If property is an interface type like IEnumerable<string> or IReadOnlyList<string>, List<string> fits too.
                if (p.PropertyType.IsInterface)
                {
                    p.SetValue(obj, tmp);
                    newValue = tmp;
                    return true;
                }
            }
        }

        return false;
    }

    public static List<string> FindEmptyProperties<T>(T obj)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanRead);

        var empties = new List<string>();
        foreach (var p in props)
        {
            var val = p.GetValue(obj);

            // If it's a string-collection, trim leading "" before checking emptiness
            if (val != null && typeof(IEnumerable<string>).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string))
            {
                TrimLeadingEmptyStringForStringCollections(obj, p, val, out val);
            }

            if (IsEmpty(val, p.PropertyType))
                empties.Add(p.Name);
        }
        return empties;
    }

    public static List<string> CleanEmptiesToNull<T>(T obj)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanRead && p.CanWrite);

        var cleaned = new List<string>();

        foreach (var p in props)
        {
            var val = p.GetValue(obj);

            // If it's a string-collection, trim leading "" first
            if (val != null && typeof(IEnumerable<string>).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string))
            {
                TrimLeadingEmptyStringForStringCollections(obj, p, val, out val);
            }

            if (!IsEmpty(val, p.PropertyType)) continue;

            bool isNullableValueType = Nullable.GetUnderlyingType(p.PropertyType) != null;
            bool isRefType = !p.PropertyType.IsValueType;

            if (isRefType || isNullableValueType)
            {
                p.SetValue(obj, null);
                cleaned.Add(p.Name);
            }
        }

        return cleaned;
    }
}
