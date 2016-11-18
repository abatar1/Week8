using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Week8
{
    public class CSVReader : IDisposable
    {
        private StreamReader stream;

        public CSVReader(string filename)
        {
            stream = new StreamReader(filename);                
        }

        #region Additional methods
        private static string[] ParseHeader(string header)
        {
            return header
                .Replace("\"", string.Empty)
                .Split(',');
        }

        private static string[] ParseValues(string values)
        {
            if (values == null) return null;

            return values.Split(',');
        }

        private static object ExpectedConvert<TType>(TType value, List<Type> expectedTypes)
        {
            if ((value as string) == "NA") return null;
            foreach (var type in expectedTypes)
            {
                var converter = TypeDescriptor.GetConverter(type);
                if (converter.CanConvertFrom(typeof(TType)))
                {
                    try
                    {
                        object newValue = converter.ConvertFrom(value);
                        if (newValue != null) return newValue;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            throw new NotSupportedException();
        }

        private List<PropertyInfo> SetProperties(Type type, BindingFlags bindingFlags)
        {
            var header = ParseHeader(stream.ReadLine());
            var properties = type.GetProperties(bindingFlags)
               .Where(t => header.Contains(t.Name))
               .ToArray();
            var sortProperties = new List<PropertyInfo>();
            foreach (var param in header)
                sortProperties.Add(properties
                    .Where(f => f.Name == param)
                    .Single());
            return sortProperties;
        }

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
      
        #endregion
        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    stream.Close();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        private IEnumerable<TResult> Read<TResult>(Func<string[], TResult> processor)
        {
            while (true)
            {
                var values = ParseValues(stream.ReadLine());
                if (values == null)
                    yield break;

                yield return processor(values);
            }
        }

        public IEnumerable<string[]> Read1()
        {
            Func<string[], string[]> processor = (values) =>
            {
                return values
                    .Select(x => x == "NA" ? null : x)
                    .ToArray();
            };
            return Read(processor);
        }

        public IEnumerable<TType> Read2<TType>()
            where TType : class, new()
        {
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            var properties = SetProperties(typeof(TType), bindingFlags);

            Func<string[], TType> processor = (values) =>
            {
                var obj = new TType();
                for (int i = 0; i < values.Length; i++)
                {
                    var rawType = properties[i].PropertyType;
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
                    properties[i].SetValue(obj, value);
                }
                return obj;
            };
            return Read(processor);
        }

        public IEnumerable<Dictionary<string, object>> Read3()
        {
            var header = ParseHeader(stream.ReadLine());
            Func<string[], Dictionary<string, object>> processor = (values) =>
            {
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < header.Length; i++)
                {
                    var expectedTypes = new List<Type> { typeof(int), typeof(double), typeof(string) };
                    var value = ExpectedConvert(values[i], expectedTypes);
                    dict.Add(header[i], value);
                }
                return dict;
            };
            return Read(processor);
        }

        public IEnumerable<dynamic> Read4()
        {
            var header = ParseHeader(stream.ReadLine());
            Func<string[], dynamic> processor = (values) =>
            {
                dynamic obj = new ExpandoObject();
                for (int i = 0; i < header.Length; i++)
                {
                    var expectedTypes = new List<Type> { typeof(int), typeof(double), typeof(string) };
                    var value = ExpectedConvert(values[i], expectedTypes);
                    ((IDictionary<string, object>)obj)[header[i]] = value;
                }
                return obj;
            };
            return Read(processor);
        }        
    }
}
