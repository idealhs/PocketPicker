using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PockerPicker
{
    internal static class RulesReader
    {
        public static Dictionary<string, string> GetDictFromFile(string fullFileName)
        {
            try
            {
                JObject o = JObject.Parse(File.ReadAllText(fullFileName));

                var json = o.ToString();

                var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                return res;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Read Rules ERROR : {ex.Message}");
                throw;
            }
        }

    }
}
