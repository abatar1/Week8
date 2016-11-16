using System;
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
                var csvFile = CSVFile.RawParseFile(stream);
             
                foreach (var line in csvFile.Content)
                    yield return line
                        .Select(x => x == "NA" ? null : x)
                        .ToArray();          
            }
        }

        public static IEnumerable<TType> Read2<TType>(string filename)
            where TType : class, new()
        {
            using (var stream = new StreamReader(filename))
            {
                var csvFile = CSVFile.RawParseFile(stream);

                var bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
                var properties = typeof(TType).GetProperties(bindingFlags)
                    .Where(t => csvFile.Header.Contains(t.Name))
                    .ToArray();

                var sortProperties = new List<PropertyInfo>();
                foreach (var param in csvFile.Header)
                    sortProperties.Add(properties
                        .Where(f => f.Name == param)
                        .Single());
                
                foreach (var line in csvFile.Content)
                {
                    var obj = new TType();

                    for (int i = 0; i < csvFile.ParamsCount; i++)
                    {
                        var rawType = sortProperties[i].PropertyType;
                        object value;
                        if (line[i] == "NA")
                            if (IsNullableType(rawType))
                                value = null;
                            else
                                throw new ArgumentException();
                        else
                        {
                            var vType = Nullable.GetUnderlyingType(rawType) ?? rawType;
                            value = Convert.ChangeType(line[i], vType);
                        }

                        sortProperties[i].SetValue(obj, value);
                    }
                    yield return obj;
                }               
            }
        }

        public static IEnumerable<Dictionary<string, object>> Read3(string filename)
        {
            using (var stream = new StreamReader(filename))
            {
                var csvFile = CSVFile.RawParseFile(stream);

                foreach (var line in csvFile.Content)
                {
                    var dict = new Dictionary<string, object>();
                    for (int i = 0; i < csvFile.ParamsCount; i++)
                    {
                        var value = ExpectedConvert(line[i]);
                        dict.Add(csvFile.Header[i], value);
                    }
                    yield return dict;
                }
            }
        }

        public static IEnumerable<dynamic> Read4(string filename)
        {
            using (var stream = new StreamReader(filename))
            {
                var csvFile = CSVFile.RawParseFile(stream);

                foreach (var line in csvFile.Content)
                {
                    dynamic obj = new ExpandoObject();
                    for (int i = 0; i < csvFile.ParamsCount; i++)
                    {
                        var value = ExpectedConvert(line[i]);
                        ((IDictionary<string, object>)obj)[csvFile.Header[i]] = value;
                    }
                    yield return obj;
                }                
            }
        }
    }
}
