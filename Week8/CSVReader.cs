﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Week8
{
    public class CSVReader
    {
        private static string[] ParseHeader(string header)
        {
            return header.Replace("\"", string.Empty).Split(',');
        }

        private static string[] ParseValues(string values)
        {
            if (values == null) return null;

            return values.Split(',');
        }

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static object ExpectedConvert(string stringValue)
        {
            if (stringValue == "NA") return null;
            var expectedTypes = new List<Type> { typeof(int), typeof(double), typeof(string) };
            foreach (var type in expectedTypes)
            {
                TypeConverter converter = TypeDescriptor.GetConverter(type);
                if (converter.CanConvertFrom(typeof(string)))
                {
                    try
                    {
                        object newValue = converter.ConvertFromInvariantString(stringValue);
                        if (newValue != null)
                        {
                            return newValue;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return null;
        }

        public static IEnumerable<string[]> Read1(string filename)
        {
            using (var stream = new StreamReader(filename))
            {
                while (true)
                {
                    var values = ParseValues(stream.ReadLine());
                    if (values == null)
                        yield break;

                    yield return values
                        .Select(x => x == "NA" ? null : x)
                        .ToArray();
                }
            }
        }

        public static IEnumerable<TType> Read2<TType>(string filename)
            where TType : class, new()
        {
            using (var stream = new StreamReader(filename))
            {
                var setParams = ParseHeader(stream.ReadLine());

                var bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
                var properties = typeof(TType).GetProperties(bindingFlags)
                    .Where(t => setParams.Contains(t.Name))
                    .ToArray();

                var sortProperties = new List<PropertyInfo>();
                foreach (var param in setParams)
                    sortProperties.Add(properties
                        .Where(f => f.Name == param)
                        .Single());

                var values = ParseValues(stream.ReadLine());
                while (values != null)
                {
                    var obj = new TType();
                    for (int i = 0; i < setParams.Length; i++)
                    {
                        var rawType = sortProperties[i].PropertyType;
                        object value;
                        if (values[i] == "NA")
                            if (IsNullableType(rawType))
                                value = null;
                            else
                                throw new ArgumentException();
                        else
                        {
                            var vType = Nullable.GetUnderlyingType(rawType) ?? rawType;
                            value = Convert.ChangeType(values[i], vType);
                        }

                        sortProperties[i].SetValue(obj, value);
                    }
                    values = ParseValues(stream.ReadLine());
                    yield return obj;
                }
            }
        }

        public static IEnumerable<Dictionary<string, object>> Read3(string filename)
        {
            using (var stream = new StreamReader(filename))
            {
                var setParams = ParseHeader(stream.ReadLine());
                var values = ParseValues(stream.ReadLine());

                while (values != null)
                {
                    var dict = new Dictionary<string, object>();
                    for (int i = 0; i < setParams.Length; i++)
                    {
                        var value = ExpectedConvert(values[i]);
                        dict.Add(setParams[i], value);
                    }
                    values = ParseValues(stream.ReadLine());
                    yield return dict;
                }
            }
        }

        public static IEnumerable<dynamic> Read4(string filename)
        {
            using (var stream = new StreamReader(filename))
            {
                var setParams = ParseHeader(stream.ReadLine());
                var values = ParseValues(stream.ReadLine());

                while (values != null)
                {
                    dynamic obj = new ExpandoObject();
                    for (int i = 0; i < setParams.Length; i++)
                    {
                        var value = ExpectedConvert(values[i]);
                        ((IDictionary<string, object>)obj)[setParams[i]] = value;
                    }
                    values = ParseValues(stream.ReadLine());
                    yield return obj;
                }
            }
        }
    }
}
