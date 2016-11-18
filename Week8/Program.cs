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
            using (var csv = new CSVReader(filename))
            {
                var a = csv.Read1().ToList();
            }
            using (var csv = new CSVReader(filename))
            {
                var a = csv.Read2<Filler>().ToList();
            }
            using (var csv = new CSVReader(filename))
            {
                var c = csv.Read3().ToList();
            }
            using (var csv = new CSVReader(filename))
            {
                var d = csv.Read4().Where(z => z.Ozone > 10).Select(z => z.Wind).ToList();
            }
        }
    }
}
