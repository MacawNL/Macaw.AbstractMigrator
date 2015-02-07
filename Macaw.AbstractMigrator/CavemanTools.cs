using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Macaw.AbstractMigrator
{
    public static class AttributeUtils
    {
        /// <summary>
        /// Returns all custom attributes of specified type
        /// </summary>
        /// <typeparam name="T">Attribute</typeparam>
        /// <param name="provider">Custom attributes provider</param>
        /// <returns></returns>
        //public static T[] GetCustomAttributes<T>(this ICustomAttributeProvider provider) where T : Attribute
        //{
        //    return GetCustomAttributes<T>(provider, true);
        //}

        /// <summary>
        /// Returns all custom attributes of specified type
        /// </summary>
        /// <typeparam name="T">Attribute</typeparam>
        /// <param name="provider">Custom attributes provider</param>
        /// <param name="inherit">When true, look up the hierarchy chain for custom attribute </param>
        /// <returns></returns>
        public static T[] GetCustomAttributes<T>(this ICustomAttributeProvider provider, bool inherit) where T : Attribute
        {
            if (provider == null)
                throw new ArgumentNullException("provider");
            T[] attributes = provider.GetCustomAttributes(typeof(T), inherit) as T[];
            if (attributes == null)
            {
                return new T[0];
            }
            return attributes;
        }

        /// <summary>
        /// Gets a single or the first custom attribute of specified type
        /// </summary>
        /// <typeparam name="T">Attribute</typeparam>
        /// <param name="memberInfo">Custom Attribute provider</param>
        /// <returns></returns>
        public static T GetSingleAttribute<T>(this ICustomAttributeProvider memberInfo) where T : Attribute
        {
            if (memberInfo == null) throw new ArgumentNullException("memberInfo");
            var list = memberInfo.GetCustomAttributes(typeof(T), false);
            if (list.Length > 0) return (T)list[0];
            return null;
        }

        public static bool HasCustomAttribute<T>(this ICustomAttributeProvider mi) where T : Attribute
        {
            return mi.GetSingleAttribute<T>() != null;
        }
    }

    public static class AssertionsExtensions
    {
         public static void MustNotBeNull<T>(this T param,string paramName=null) where T:class
         {
             if (param == null) throw new ArgumentNullException(paramName??string.Empty);
         }

        public static void MustNotBeEmpty(this string arg,string paramName=null)
        {
            if (string.IsNullOrWhiteSpace(arg)) throw new FormatException(string.Format("Argument '{0}' must not be null, empty or whitespaces",paramName??""));
        }

        public static void MustNotBeEmpty<T>(this IEnumerable<T> list,string paramName=null)
        {
            if (list.IsNullOrEmpty()) throw new ArgumentException("The collection must contain at least one element",paramName??"");
        }
            
        public static void MustMatch(this string source,string regex,RegexOptions options=RegexOptions.None)
        {
            if (source.IsNullOrEmpty() || !Regex.IsMatch(source,regex,options)) throw new FormatException(string.Format("Argument doesn't match expression '{0}'",regex));
        }

        /// <summary>
        /// Throws if the value can't be used as-is in an url
        /// </summary>
        /// <param name="source"></param>
        //public static void MustBeUrlFriendly(this string source)
        //{
        //    if (!source.IsNullOrEmpty())
        //    {
        //        if (source.MakeSlug().Equals(source,StringComparison.OrdinalIgnoreCase)) return;
        //    }
        //    throw new FormatException("Provided string value must be url friendly");
        //}


        /// <summary>
        /// Value type must be of specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public static void MustBe<T>(this object value)
        {
            var tp = typeof (T);
            bool ex = false;
            if (value == null)
            {
                if (tp.IsClass) return;
                ex = true;
            }
            if (ex || (value.GetType()!=tp)) throw new ArgumentException("Argument must be of type '{0}'".ToFormat(tp));
        }

        public static void MustComplyWith<T>(this T arg,Func<T, bool> condition,string msg)
        {
            msg.MustNotBeEmpty();
            if (!condition(arg))
            {
                throw new ArgumentException(msg);
            }
        }

        /// <summary>
        /// Arugment must implement interface T
        /// </summary>
        /// <typeparam name="T">Inerface type</typeparam>
        /// <param name="value"></param>
        public static void MustImplement<T>(this object value)
        {
            value.MustNotBeNull("value");
            var tp = typeof (T);
            if (!tp.IsInterface) throw new ArgumentException("'{0}' is not an interface".ToFormat(tp));
            var otype = value.GetType();
            
            if (value is Type)
            {
                otype= value as Type;
            }

            if (!otype.Implements(tp)) throw new ArgumentException("Argument must implement '{0}'".ToFormat(tp));
        }

        /// <summary>
        /// Argument must be an implementation or subclass of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public static void MustDeriveFrom<T>(this object value)
        {
            value.MustNotBeNull("value");
            var tp = typeof(T);
            var otype = value.GetType();

            if (value is Type)
            {
                otype = value as Type;
            }

            if (!otype.DerivesFrom(tp)) throw new ArgumentException("Argument must derive from '{0}'".ToFormat(tp)); 
        }

        /// <summary>
        /// List must not be empty and must have non-null values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="throwWhenNullValues"></param>
        public static void MustHaveValues<T>(this IEnumerable<T> list,bool throwWhenNullValues=true) where T : class
        {
            list.MustNotBeEmpty();
            
            if (throwWhenNullValues)
            {
                if (list.Any(v => v == null))
                {
                    throw new ArgumentException("The collection is null, empty or it contains null values");
                }
            }            
        }
    }

    public static class ListUtils
    {
        /// <summary>
        /// Checks if 2 enumerables have the same elements in the same order
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static bool HasTheSameElementsAs<T>(this IEnumerable<T> first, IEnumerable<T> second)
        {
            first.MustNotBeNull();
            second.MustNotBeNull();

            var cnt1 = first.Count();
            if (cnt1 != second.Count()) return false;
            T item1 = default(T);
            T item2 = default(T);
            for (int i = 0; i < cnt1; i++)
            {
                item1 = first.Skip(i).Take(1).First();
                item2 = second.Skip(i).Take(1).First();
                if (!item1.Equals(item2)) return false;
            }
            return true;
        }
        /// <summary>
        /// Compares two sequences and returns the added or removed items.
        /// </summary>
        /// <typeparam name="T">Implements IEquatable</typeparam>
        /// <param name="fresh">Recent sequence</param>
        /// <param name="old">Older sequence used as base of comparison</param>
        /// <returns></returns>
        public static IModifiedSet<T> Compare<T>(this IEnumerable<T> fresh, IEnumerable<T> old) where T : IEquatable<T>
        {
            if (fresh == null) throw new ArgumentNullException("fresh");
            if (old == null) throw new ArgumentNullException("old");
            var mods = new ModifiedSet<T>();

            foreach (var item in old)
            {
                if (!fresh.Contains(item)) mods.RemovedItem(item);
            }

            foreach (var item in fresh)
            {
                if (!old.Contains(item)) mods.AddedItem(item);
            }
            return mods;
        }

        /// <summary>
        /// Compares two sequences and returns the added or removed items.
        /// Use this when T doesn't implement IEquatable
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="fresh">Recent sequence</param>
        /// <param name="old">Older sequence used as base of comparison</param>
        /// <param name="match">function to check equality</param>
        /// <returns></returns>
        public static IModifiedSet<T> Compare<T>(this IEnumerable<T> fresh, IEnumerable<T> old, Func<T, T, bool> match)
        {
            if (fresh == null) throw new ArgumentNullException("fresh");
            if (old == null) throw new ArgumentNullException("old");
            if (match == null) throw new ArgumentNullException("match");
            var mods = new ModifiedSet<T>();

            foreach (var item in old)
            {
                if (!fresh.Any(d => match(d, item))) mods.RemovedItem(item);
            }

            foreach (var item in fresh)
            {
                if (!old.Any(d => match(d, item))) mods.AddedItem(item);
            }
            return mods;
        }

        /// <summary>
        /// Compares two sequences and returns the result.
        /// This special case method is best used when you have identifiable objects that can change their content/value but not their id.
        /// </summary>
        /// <typeparam name="T">Implements IEquatable</typeparam>
        /// <param name="fresh">Recent sequence</param>
        /// <param name="old">Older sequence used as base of comparison</param>
        /// <param name="detectChange">Delegate to determine if the items are identical.
        /// First parameter is new item, second is the item used as base for comparison</param>
        /// <returns></returns>
        //public static IModifiedSet<T> WhatChanged<T>(this IEnumerable<T> fresh, IEnumerable<T> old, Func<T, T, bool> detectChange) where T : IEquatable<T>
        //{
        //    if (fresh == null) throw new ArgumentNullException("fresh");
        //    if (old == null) throw new ArgumentNullException("old");
        //    if (detectChange == null) throw new ArgumentNullException("detectChange");
        //    var mods = new ModifiedSet<T>();

        //    foreach (var item in old)
        //    {
        //        if (!fresh.Any(d => d.Equals(item))) mods.RemovedItem(item);
        //    }

        //    foreach (var item in fresh)
        //    {
        //        if (!old.Any(d => d.Equals(item))) mods.AddedItem(item);
        //        else
        //        {
        //            var oldItem = old.First(d => d.Equals(item));
        //            if (detectChange(item, oldItem))
        //            {
        //                mods.ModifiedItem(oldItem, item);
        //            }
        //        }
        //    }
        //    return mods;
        //}

        /// <summary>
        /// Updates the old collection with new items, while removing the inexistent.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="old"></param>
        /// <param name="fresh"></param>
        /// <returns></returns>
        //public static void Update<T>(this IList<T> old, IEnumerable<T> fresh) where T : IEquatable<T>
        //{
        //    if (old == null) throw new ArgumentNullException("old");
        //    if (fresh == null) throw new ArgumentNullException("fresh");
        //    var diff = fresh.Compare(old);
        //    foreach (var item in diff.Removed)
        //    {
        //        old.Remove(item);
        //    }
        //    foreach (var item in diff.Added)
        //    {
        //        old.Add(item);
        //    }
        //}

        /// <summary>
        /// Updates the old collection with new items, while removing the inexistent.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="old"></param>
        /// <param name="fresh"></param>
        /// <returns></returns>
        //public static void Update<T>(this IList<T> old, IEnumerable<T> fresh, Func<T, T, bool> isEqual)
        //{
        //    if (old == null) throw new ArgumentNullException("old");
        //    if (fresh == null) throw new ArgumentNullException("fresh");
        //    var diff = fresh.Compare(old, isEqual);

        //    foreach (var item in diff.Removed)
        //    {
        //        var i = old.Where(d => isEqual(d, item)).Select((d, idx) => idx).First();
        //        old.RemoveAt(i);
        //    }
        //    foreach (var item in diff.Added)
        //    {
        //        old.Add(item);
        //    }
        //}

        /// <summary>
        /// Checks if a collection is null or empty duh!
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="items">collection</param>
        /// <returns></returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> items)
        {
            return items == null || !items.Any();
        }

        /// <summary>
        /// Gets typed value from dictionary or a default value if key is missing
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defValue">Value to return if dictionary doesn't contain the key</param>
        /// <returns></returns>
        //public static T GetValue<T>(this IDictionary<string, object> dic, string key, T defValue = default(T))
        //{
        //    if (dic.ContainsKey(key)) return dic[key].Cast<T>();
        //    return defValue;
        //}

        //public static bool AddIfNotPresent<T>(this IList<T> list, T item)
        //{
        //    list.MustNotBeNull();
        //    if (!list.Contains(item))
        //    {
        //        list.Add(item);
        //        return true;
        //    }
        //    return false;
        //}
        /// <summary>
        /// Returns number of items removed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        //public static int RemoveAll<T>(this IList<T> items, Func<T, bool> predicate)
        //{
        //    items.MustNotBeEmpty();
        //    predicate.MustNotBeNull();
        //    var removed = 0;
        //    for (int i = items.Count - 1; i >= 0; i--)
        //    {
        //        if (predicate(items[i]))
        //        {
        //            items.RemoveAt(i);
        //            removed++;
        //        }
        //    }
        //    return removed;
        //}
    }

    //public static class ObjectExtend
    //{
    //    private static ConcurrentDictionary<Type, TypeInfo> _typeDicts;
    //    /// <summary>
    //    /// Creates dictionary from object properties.
    //    /// </summary>
    //    /// <param name="value">Object</param>
    //    /// <returns></returns>
    //    public static IDictionary<string, object> ToDictionary(this object value)
    //    {
    //        if (_typeDicts == null)
    //        {
    //            _typeDicts = new ConcurrentDictionary<Type, TypeInfo>();
    //        }

    //        TypeInfo info;
    //        var tp = value.GetType();
    //        if (tp == typeof(ExpandoObject))
    //        {
    //            return (IDictionary<string, object>)value;
    //        }

    //        if (!_typeDicts.TryGetValue(tp, out info))
    //        {
    //            var allp = tp.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);

    //            //lambda
    //            var dict = Expression.Parameter(typeof(IDictionary<string, object>), "dict");
    //            var inst = Expression.Parameter(typeof(object), "obj");

    //            var lblock = new List<Expression>(allp.Length);


    //            for (int i = 0; i < allp.Length; i++)
    //            {
    //                var prop = allp[i];
    //                var indexer = Expression.Property(dict, "Item", Expression.Constant(prop.Name));
    //                lblock.Add(
    //                    Expression.Assign(indexer,
    //                        Expression.ConvertChecked(
    //                            Expression.Property(
    //                               Expression.ConvertChecked(inst, tp), prop.Name), typeof(object))
    //                        ));
    //            }
    //            var body = Expression.Block(lblock);
    //            var lambda = Expression.Lambda(Expression.GetActionType(typeof(IDictionary<string, object>), typeof(object)), body, dict, inst);

    //            info = new TypeInfo(allp.Length, lambda.Compile());
    //            _typeDicts.TryAdd(tp, info);
    //        }

    //        return info.Update(value.ConvertTo(tp));
    //    }

        class TypeInfo
        {
            private readonly int _size;
            private readonly Delegate _del;

            public TypeInfo(int size, Delegate del)
            {
                _size = size;
                _del = del;
            }

            public Dictionary<string, object> Update(object o)
            {
                var d = new Dictionary<string, object>(_size);
                (_del as Action<IDictionary<string, object>, object>)(d, o);
                return d;
            }
        }


        ///// <summary>
        /////  Shallow copies source object into destination, only public properties are copied. Use CopyOptionsAttribute to mark the properties you want ignored.
        ///// Use Automapper for heavy duty mapping
        ///// </summary>
        ///// <seealso cref="CopyOptionsAttribute"/>
        ///// <typeparam name="T">Destination type must have parameterless constructor</typeparam>
        ///// <param name="source">Object to copy</param>
        //public static T CopyTo<T>(this object source) where T :class, new() 
        //{
        //    var destination = new T();
        //    source.CopyTo(destination);
        //    return destination;
        //}


        ///// <summary>
        ///// Shallow copies source object into destination, only public properties are copied. For ocasional use.
        ///// Use Automapper for heavy duty mapping
        ///// </summary>
        ///// <exception cref="ArgumentNullException">If source or destination are null</exception>
        ///// <typeparam name="T">Destination Type</typeparam>
        ///// <param name="source">Object to copy from</param>
        ///// <param name="destination">Object to copy to. Unmatching or read-only properties are ignored</param>
        //public static void CopyTo<T>(this object source, T destination) where T : class
        //{
        //    if (source == null) throw new ArgumentNullException("source");
        //    if (destination == null) throw new ArgumentNullException("destination");
        //    var srcType = source.GetType();
        //    var attr = destination.GetType().GetSingleAttribute<CopyOptionsAttribute>();
        //    if (attr != null)
        //    {
        //        if (attr.IgnoreProperty) ;
        //    }

        //    foreach (var destProperty in destination.GetType().GetProperties(BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.FlattenHierarchy))
        //    {
        //        if (!destProperty.CanWrite) continue;

        //        var pSource = srcType.GetProperty(destProperty.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
        //        if (pSource == null) continue;
        //        var o = pSource.GetValue(source, null);
        //        if (!pSource.PropertyType.Equals(destProperty.PropertyType))
        //        {
        //            o = ConvertTo(o, destProperty.PropertyType);
        //        }
        //        destProperty.SetValue(destination, o, null);
        //    }
        //}


        /// <summary>
        /// Converts object to type.
        /// Supports conversion to Enum, DateTime,TimeSpan and CultureInfo
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        /// <param name="data">Object to be converted</param>
        /// <param name="tp">Type to convert to</param>
        /// <returns></returns>
        //public static object ConvertTo(this object data, Type tp)
        //{
        //    if (data == null)
        //    {
        //        if (tp.IsValueType && !tp.IsNullable())
        //        {
        //            throw new InvalidCastException("Cant convert null to a value type");
        //        }
        //        return null;
        //    }

        //    var otp = data.GetType();
        //    if (otp.Equals(tp)) return data;
        //    if (tp.IsEnum)
        //    {
        //        if (data is string)
        //        {
        //            return Enum.Parse(tp, data.ToString());
        //        }
        //        var o = Enum.ToObject(tp, data);
        //        return o;
        //    }

        //    if (tp.IsValueType)
        //    {
        //        if (tp == typeof(TimeSpan))
        //        {
        //            return TimeSpan.Parse(data.ToString());
        //        }

        //        if (tp == typeof(DateTime))
        //        {
        //            if (data is DateTimeOffset)
        //            {
        //                return data.Cast<DateTimeOffset>().DateTime;
        //            }
        //            return DateTime.Parse(data.ToString());
        //        }

        //        if (tp == typeof(DateTimeOffset))
        //        {
        //            if (data is DateTime)
        //            {
        //                var dt = (DateTime)data;
        //                return new DateTimeOffset(dt);
        //            }
        //            return DateTimeOffset.Parse(data.ToString());
        //        }

        //        if (tp.IsNullable())
        //        {
        //            var under = Nullable.GetUnderlyingType(tp);
        //            return data.ConvertTo(under);
        //        }
        //    }
        //    else if (tp == typeof(CultureInfo)) return new CultureInfo(data.ToString());

        //    return System.Convert.ChangeType(data, tp);
        //}

        ///// <summary>
        /////	Tries to convert the object to type.
        ///// </summary>
        ///// <exception cref="InvalidCastException"></exception>
        ///// <exception cref="FormatException"></exception>
        ///// <typeparam name="T">Type to convert to</typeparam>
        ///// <param name="data">Object</param>
        ///// <returns></returns>
        //public static T ConvertTo<T>(this object data)
        //{
        //    var tp = typeof(T);
        //    var temp = (T)ConvertTo(data, tp);
        //    return temp;
        //}



        /// <summary>
        ///	Tries to convert the object to type.
        /// If it fails it returns the specified default value.
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <param name="data">Object</param>
        /// <param name="defaultValue">IF not set , the default for T is used</param>
        /// <returns></returns>
        //public static T SilentConvertTo<T>(this object data, T defaultValue = default(T))
        //{
        //    var tp = typeof(T);
        //    try
        //    {
        //        var temp = (T)ConvertTo(data, tp);
        //        return temp;
        //    }
        //    catch (InvalidCastException)
        //    {
        //        return defaultValue;
        //    }
        //}


    //    /// <summary>
    //    /// Shorthand for lazy people to cast an object to a type
    //    /// </summary>
    //    /// <typeparam name="T"></typeparam>
    //    /// <param name="o"></param>
    //    /// <returns></returns>
    //    public static T As<T>(this object o) where T : class
    //    {
    //        return o as T;
    //    }

    //    /// <summary>
    //    /// Shorthand for lazy people to cast an object to a type
    //    /// </summary>
    //    /// <typeparam name="T"></typeparam>
    //    /// <param name="o"></param>
    //    /// <returns></returns>
    //    public static T Cast<T>(this object o)
    //    {
    //        return (T)o;
    //    }

    //    /// <summary>
    //    /// Shortcut for 'object is type'
    //    /// </summary>
    //    /// <typeparam name="T"></typeparam>
    //    /// <param name="o"></param>
    //    /// <returns></returns>
    //    public static bool Is<T>(this object o)
    //    {
    //        if (o is Type)
    //        {
    //            return (Type)o == typeof(T);
    //        }
    //        return o is T;
    //    }
    //}

    public static class TypeExtensions
    {
        /// <summary>
        /// Used for checking if a class implements an interface
        /// </summary>
        /// <typeparam name="T">Interface</typeparam>
        /// <param name="type">Class Implementing the interface</param>
        /// <returns></returns>
        public static bool Implements<T>(this Type type)
        {
            type.MustNotBeNull();
            return type.Implements(typeof(T));
        }

        /// <summary>
        /// Creates a new instance of type using a public parameterless constructor
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object CreateInstance(this Type type)
        {
            type.MustNotBeNull();
            return Activator.CreateInstance(type);
        }

        /// <summary>
        /// Used for checking if a class implements an interface
        /// </summary>
        /// <param name="type">Class Implementing the interface</param>
        /// <param name="interfaceType">Type of an interface</param>
        /// <returns></returns>
        public static bool Implements(this Type type, Type interfaceType)
        {
            type.MustNotBeNull();
            interfaceType.MustNotBeNull();
            if (!interfaceType.IsInterface) throw new ArgumentException(String.Format("The generic type '{0}' is not an interface", interfaceType));
            return interfaceType.IsAssignableFrom(type);
        }

        /// <summary>
        /// True if the type implements of extends T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool DerivesFrom<T>(this Type type)
        {
            return type.DerivesFrom(typeof(T));
        }

        /// <summary>
        /// True if the type implements of extends parent. 
        /// Doesn't work with generics
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static bool DerivesFrom(this Type type, Type parent)
        {
            type.MustNotBeNull();
            parent.MustNotBeNull();
            return parent.IsAssignableFrom(type);
        }

        public static bool CheckIfAnonymousType(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="interfaceName">The intuitive interface name</param>
        /// <param name="genericType">Interface's generic arguments types</param>
        /// <returns></returns>
        public static bool ImplementsGenericInterface(this object o, string interfaceName, params Type[] genericType)
        {
            Type tp = o.GetType();
            if (o is Type)
            {
                tp = (Type)o;
            }
            var interfaces = tp.GetInterfaces().Where(i => i.IsGenericType && i.Name.StartsWith(interfaceName));
            if (genericType.Length == 0)
            {
                return interfaces.Any();
            }

            return interfaces.Any(
                i =>
                {
                    var args = i.GetGenericArguments();
                    return args.HasTheSameElementsAs(genericType);
                });
        }

        public static bool ExtendsGenericType(this Type tp, string typeName, params Type[] genericArgs)
        {
            tp.MustNotBeNull();
            typeName.MustNotBeEmpty();
            if (tp.BaseType == null) return false;
            var baseType = tp.BaseType;
            if (!baseType.IsGenericType || !baseType.Name.StartsWith(typeName)) return false;
            if (genericArgs.Length > 0)
            {
                return baseType.GetGenericArguments().HasTheSameElementsAs(genericArgs);
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tp">Generic type</param>
        /// <param name="index">0 based index of the generic argument</param>
        /// <exception cref="InvalidOperationException">When the type is not generic</exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public static Type GetGenericArgument(this Type tp, int index = 0)
        {
            tp.MustNotBeNull();
            if (!tp.IsGenericType) throw new InvalidOperationException("Provided type is not generic");
            var all = tp.GetGenericArguments();
            if (index >= all.Length)
            {
                throw new ArgumentException("There is no generic argument at the specified index", "index");
            }
            return all[index];
        }

        /// <summary>
        /// Checks if type is a reference but also not
        ///  a string, array, Nullable, enum
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsUserDefinedClass(this Type type)
        {
            type.MustNotBeNull();
            if (!type.IsClass) return false;
            if (Type.GetTypeCode(type) != TypeCode.Object) return false;
            if (type.IsArray) return false;
            if (type.IsNullable()) return false;
            return true;
        }

        /// <summary>
        /// This always returns false if the type is taken from an instance.
        /// That is always use typeof
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNullable(this Type type)
        {
            type.MustNotBeNull();
            return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        public static bool IsNullableOf(this Type type, Type other)
        {
            type.MustNotBeNull();
            other.MustNotBeNull();
            return type.IsNullable() && type.GetGenericArguments()[0].Equals(other);
        }

        public static bool IsNullableOf<T>(this Type type)
        {
            return type.IsNullableOf(typeof(T));
        }

        public static bool CanBeCastTo<T>(this Type type)
        {
            if (type == null) return false;
            return CanBeCastTo(type, typeof(T));
        }

        public static bool CanBeCastTo(this Type type, Type other)
        {
            if (type == null) return false;
            if (type == other) return true;
            return other.IsAssignableFrom(type);
        }

        /// <summary>
        /// Returns the version of assembly containing type
        /// </summary>
        /// <returns></returns>
        public static Version AssemblyVersion(this Type tp)
        {
            return Assembly.GetAssembly(tp).GetName().Version;
        }
        /// <summary>
        /// Returns the full name of type, including assembly but not version, public key etc, i.e: namespace.type, assembly
        /// </summary>
        /// <param name="t">Type</param>
        /// <returns></returns>
        public static string GetFullTypeName(this Type t)
        {
            if (t == null) throw new ArgumentNullException("t");
            return String.Format("{0}, {1}", t.FullName, Assembly.GetAssembly(t).GetName().Name);
        }

        public static object GetDefault(this Type type)
        {
            if (type.IsValueType) return Activator.CreateInstance(type);
            return null;
        }

        /// <summary>
        /// Returns namespace without the assembly name
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string StripNamespaceAssemblyName(this Type type)
        {
            var asmName = type.Assembly.GetName().Name;
            return type.Namespace.Substring(asmName.Length + 1);
        }

    }

    public static class StringUtils
    {
        /// <summary>
        /// Creates url friendly slug of a string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string MakeSlug(this string text)
        {
            if (String.IsNullOrEmpty(text)) return String.Empty;

            // to lowercase, trim extra spaces
            text = text.ToLower().Trim();

            var len = text.Length;
            var sb = new StringBuilder(len);
            bool prevdash = false;
            char c;

            for (int i = 0; i < text.Length; i++)
            {
                c = text[i];
                if (c == ' ' || c == ',' || c == '.' || c == '/' || c == '\\' || c == '-')
                {
                    if (!prevdash)
                    {
                        sb.Append('-');
                        prevdash = true;
                    }
                }
                else if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                    prevdash = false;
                }
                if (i == 80) break;
            }

            text = sb.ToString();
            // remove trailing dash, if there is one
            if (text.EndsWith("-"))
                text = text.Substring(0, text.Length - 1);
            return text;
        }

        /// <summary>
        /// Parses string to culture. Returns null if unsuccessful.
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static CultureInfo ToCulture(this string lang)
        {
            CultureInfo c = null;
            try
            {
                c = new CultureInfo(lang);
            }
            catch (ArgumentNullException)
            {

            }
            catch (ArgumentException)
            {

            }

            return c;
        }



        /// <summary>
        /// Cuts the string to the specified length
        /// </summary>
        /// <param name="value">string</param>
        /// <param name="length">maximum length</param>
        /// <returns></returns>
        public static string Cut(this string value, int length)
        {
            if (String.IsNullOrEmpty(value)) return "";
            var l = value.Length > length ? length : value.Length;
            return value.Substring(0, l);
        }

        public static string RemoveLastChar(this string value)
        {
            value.MustNotBeNull();
            if (value.Length == 0) return value;
            return value.Remove(value.Length - 1, 1);
        }


        /// <summary>
        /// Returns true if the string is empty 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="checkBlancs">trim blancs</param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this string data, bool checkBlancs = false)
        {
            if (data == null) return true;
            if (checkBlancs)
            {
                data = data.Trim();
            }
            return String.IsNullOrEmpty(data);
        }

        /// <summary>
        /// Converts strings form unicode to specified encoding
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="encoding">Encoding</param>
        /// <returns></returns>
        public static string ConvertToEncoding(this string s, Encoding encoding)
        {
            var bytes = Encoding.Unicode.GetBytes(s);
            bytes = Encoding.Convert(Encoding.Unicode, encoding, bytes);
            return Encoding.ASCII.GetString(bytes);
        }

        /// <summary>
        /// Capitalizes the first letter from string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string Capitalize(this string s)
        {
            if (String.IsNullOrEmpty(s)) return String.Empty;
            return s.Substring(0, 1).ToUpperInvariant() + s.Substring(1, s.Length - 1).ToLowerInvariant();
        }



        /// <summary>
        /// Reads the Stream as an UTF8 String
        /// </summary>
        /// <param name="data">Stream</param>
        /// <returns></returns>
        public static string ReadAsString(this Stream data)
        {
            using (var r = new StreamReader(data, Encoding.UTF8))
                return r.ReadToEnd();
        }

        /// <summary>
        /// Returns true if teh string is a valid email format.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsEmail(this string text)
        {
            return Regex.IsMatch(text, @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|"
    + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)"
    + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$");
        }

        /// <summary>
        /// Returns the first line from a multilined string
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string GetFirstLine(this string source)
        {
            if (string.IsNullOrEmpty(source)) return "";
            return source.Split('\n').FirstOrDefault();
        }

        /// <summary>
        /// Generates a random string of the specified length starting with prefix
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GenerateRandomString(this string prefix, int length)
        {
            if (prefix == null) prefix = "";
            if (length <= prefix.Length) return prefix;
            return prefix + CreateRandomString(length - prefix.Length);
        }

        /// <summary>
        /// Generates a random string (only letters) of the specified length
        /// </summary>
        /// <param name="length">Maximum string length</param>
        /// <returns></returns>
        public static string CreateRandomString(int length)
        {
            var buff = new byte[length];
            var _random = new Random();

            _random.NextBytes(buff);

            return Convert.ToBase64String(buff).Substring(0, length);

            //StringBuilder builder = new StringBuilder();
            //for (int i = 0; i < length; i++)
            //{

            //    //26 letters in the alfabet, ascii + 65 for the capital letters
            //    builder.Append(Convert.ToChar(Convert.ToInt32(Math.Floor(26 * _random.NextDouble() + 65))));

            //}
            //return builder.ToString();

        }

        public static string ToFormat(this string pattern, params object[] args)
        {
            pattern.MustNotBeEmpty();
            return String.Format(pattern, args);
        }


        public static T ToEnum<T>(this string value)
        {
            if (!typeof(T).IsEnum) throw new ArgumentException("Type '{0}' is not an enum".ToFormat(typeof(T)));
            return (T)Enum.Parse(typeof(T), value, true);
        }
    }

    public static class LinqExt
    {

        /// <summary>
        /// Executes function for each sequence item
        /// </summary>
        /// <typeparam name="TSource">Sequence</typeparam>
        /// <param name="source">Function to execute</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action
            )
        {
            if (source == null) throw new ArgumentNullException("source");
            if (action == null) throw new ArgumentNullException("action");
            foreach (var b in source)
            {
                action(b);
            }
        }

        /// <summary>
        /// Tries to cast each item to the specified type. If it fails,  it just ignores the item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<T> CastSilentlyTo<T>(this IEnumerable source) where T : class
        {
            T res = null;
            foreach (var item in source)
            {
                res = item as T;
                if (res == null) continue;
                yield return res;
            }

        }
    }

    public static class AssemblyExtensions
    {
        /// <summary>
        /// Returns public types derived from T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asm"></param>
        /// <param name="instantiable">True to return only types that can be instantiated i.e no interfaces and no abstract classes</param>
        /// <returns></returns>
        public static IEnumerable<Type> GetTypesDerivedFrom<T>(this Assembly asm, bool instantiable = false)
        {
            if (asm == null) throw new ArgumentNullException("asm");
            var res = asm.GetExportedTypes().Where(tp => (typeof(T)).IsAssignableFrom(tp));
            if (instantiable)
            {
                res = res.Where(t => !t.IsAbstract && !t.IsInterface);
            }
            return res;
        }

        [Obsolete("Use GetTypesDerivedFrom")]
        public static IEnumerable<Type> GetTypesImplementing<T>(this Assembly asm, bool instantiable = false)
        {
            return asm.GetTypesDerivedFrom<T>(instantiable);
        }

        /// <summary>
        /// Searches and instantiate types derived from T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asm"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetInstancesOfTypesDerivedFrom<T>(this Assembly asm) where T : class, new()
        {
            return asm.GetTypesDerivedFrom<T>(true).Select(t => (T)Activator.CreateInstance(t));
        }

        public static IEnumerable<Type> GetTypesWithAttribute<T>(this Assembly asm) where T : Attribute
        {
            if (asm == null) throw new ArgumentNullException("asm");
            return asm.GetExportedTypes().Where(a => a.HasCustomAttribute<T>());
        }
    }
}
