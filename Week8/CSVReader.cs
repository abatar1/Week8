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

        private IEnumerable<TResult> Read<TResult>
            (Action<string[], TResult, int> processor) where TResult : new()
        {
            while (true)
            {
                var values = ParseValues(stream.ReadLine());
                if (values == null)
                    yield break;
                var obj = new TResult();
                for (int i = 0; i < values.Length; i++)
                {
                    processor(values, obj, i);
                }
                yield return obj;
            }
        }

        public IEnumerable<string[]> Read1()
        {
            while (true)
            {
                var values = ParseValues(stream.ReadLine());
                if (values == null) yield break;

                yield return values
                    .Select(x => x == "NA" ? null : x)
                    .ToArray();
            }
        }

        public IEnumerable<TType> Read2<TType>()
            where TType : class, new()
        {
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            var properties = SetProperties(typeof(TType), bindingFlags);

            Action<string[], TType, int> processor = (values, obj, i) =>
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
            };
            return Read(processor);
        }

        public IEnumerable<Dictionary<string, object>> Read3()
        {
            var header = ParseHeader(stream.ReadLine());
            Action<string[], Dictionary<string, object>, int> processor = (values, obj, i) =>
            {
                var expectedTypes = new List<Type> { typeof(int), typeof(double), typeof(string) };
                var value = ExpectedConvert(values[i], expectedTypes);
                obj.Add(header[i], value);
            };
            return Read(processor);
        }

        public IEnumerable<dynamic> Read4()
        {
            var header = ParseHeader(stream.ReadLine());
            Action<string[], dynamic, int> processor = (values, obj, i) =>
            {
                var expectedTypes = new List<Type> { typeof(int), typeof(double), typeof(string) };
                var value = ExpectedConvert(values[i], expectedTypes);
                ((IDictionary<string, object>)obj)[header[i]] = value;
            };
            return Read(processor);
        }        
    }
}
