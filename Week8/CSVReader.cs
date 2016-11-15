using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Week8
{
    public class CSVReader
    {
        public static IEnumerable<string[]> Read1(string filename)
        {
            using (var stream = new StreamReader(filename))
            {
                while (true)
                {
                    var str = stream.ReadLine();
                    if (str == null)
                    {
                        stream.Close();
                        yield break;
                    }

                    yield return str.Split(',')
                        .Select(x => x == "NA" ? null : x)
                        .ToArray();
                }
            }
        }

        public static IEnumerable<TType> Read2<TType>(string filename)
            where TType : class
        {
            using (var stream = new StreamReader(filename))
            {
                var setParams = stream.ReadLine().Replace("\"", string.Empty).Split(',');
                var bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
                var fields = typeof(TType).GetFields(bindingFlags)
                    .Where(t => setParams.Contains(t.Name))
                    .ToArray();

                var length = fields.Length;
                List<FieldInfo> sortFields = new List<FieldInfo>();
                for (int i = 0; i < length; i++)
                {
                    sortFields.Add(fields.Where(f => f.Name == setParams[i]).Single());
                }

                while (true)
                {
                    var str = stream.ReadLine();
                    if (str == null)
                    {
                        stream.Close();
                        yield break;
                    }

                    var values = str.Split(',');
                    var obj = (TType)Activator.CreateInstance(typeof(TType));

                    for (int i = 0; i < length; i++)
                    {
                        var rawType = sortFields[i].FieldType;
                        var vType = Nullable.GetUnderlyingType(rawType) ?? rawType;
                        object value = values[i];
                        if (value.ToString() == "NA")
                            if (rawType.IsGenericType && rawType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                value = null;
                            else
                                throw new ArgumentException();
                        else
                            value = Convert.ChangeType(value, vType);

                        sortFields[i].SetValue(obj, value);
                    }
                    yield return obj;
                }
            }
        }

        private static object ExpectedConverter(string stringValue)
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

        public static IEnumerable<Dictionary<string, object>> Read3(string filename)
        {
            using (var stream = new StreamReader(filename))
            {
                var setParams = stream.ReadLine().Replace("\"", string.Empty).Split(',');
                var length = setParams.Length;

                while (true)
                {
                    var str = stream.ReadLine();
                    if (str == null)
                    {
                        stream.Close();
                        yield break;
                    }

                    var values = str.Split(',');
                    var dict = new Dictionary<string, object>();
                    for (int i = 0; i < length; i++)
                    {
                        var value = ExpectedConverter(values[i]);
                        dict.Add(setParams[i], value);
                    }
                    yield return dict;
                }
            }
        }

        public static IEnumerable<dynamic> Read4(string filename)
        {
            using (var stream = new StreamReader(filename))
            {
                var setParams = stream.ReadLine().Replace("\"", string.Empty).Split(',');
                var length = setParams.Length;

                var aBuilder =
                    AppDomain.CurrentDomain.DefineDynamicAssembly(
                        new AssemblyName("Assembly"),
                        AssemblyBuilderAccess.Run);
                var mBuilder = 
                    aBuilder.DefineDynamicModule("Module");
                var tBuilder = 
                    mBuilder.DefineType("DynamicType",
                        TypeAttributes.Public |
                        TypeAttributes.Class |
                        TypeAttributes.AutoLayout,
                        null);
                foreach (var param in setParams)
                {
                    var fieldBuilder = tBuilder.
                        DefineField(param, typeof(object), FieldAttributes.Public);
                }
                var dynamicType = tBuilder.CreateType();
                var fields = dynamicType.GetFields();
                var result = new List<object>();
                while (true)
                {
                    var str = stream.ReadLine();
                    if (str == null) break;

                    var values = str.Split(',');
                    var obj = Activator.CreateInstance(dynamicType);

                    for (int i = 0; i < length; i++)
                    {
                        var value = ExpectedConverter(values[i]);
                        fields[i].SetValue(obj, value);               
                    }
                    result.Add(obj);
                }
                return result;
            }
        }
    }
}
