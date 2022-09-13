using System.Text;

namespace PockerPicker
{
    internal static class SaveFile
    {
        public static string SavePath { get; set; }
        public static bool SaveTo(string fileName, string body)
        {
            try
            {
                body = body.Insert(0, @"{""Response"":");
                body = body.Insert(body.Length, "}"); 
                using (FileStream fs = File.Create($"{SavePath}\\{fileName}.json"))
                {
                    byte[] buffer = new UTF8Encoding(true).GetBytes(body);
                    fs.Write(buffer, 0, buffer.Length);
                }

                    return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save to File ERROR : {ex}");
                return false;
            }
        }

    }
}
