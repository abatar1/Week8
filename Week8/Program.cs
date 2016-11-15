using System.Linq;

namespace Week8
{
    public class Program
    {
        public static void Main(string[] args)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            var filename = "airquality.csv";
            var a = CSVReader.Read1(filename).ToList();
            var b = CSVReader.Read2<Filler>(filename).ToList();
            var c = CSVReader.Read3(filename).ToList();
            var d = CSVReader.Read4(filename).Where(z => z.Ozone > 10).Select(z => z.Wind).ToList();
        }
    }
}
