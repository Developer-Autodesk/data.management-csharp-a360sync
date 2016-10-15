using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ForgeSampleA360Sync
{
  public class FolderUtils
  {
    public static void EnsureFolderExists(string path)
    {
      if (!Directory.Exists(path))
        Directory.CreateDirectory(path);
    }

    public static void CreateIDFile(string path, string type, string id)
    {
      string filePath = Path.Combine(path, "_id.txt");
      if (File.Exists(filePath)) return;

      using (StreamWriter outputFile = new StreamWriter(filePath))
      {
        outputFile.WriteLine(type);
        outputFile.WriteLine(id);
      }
      File.SetAttributes(filePath, FileAttributes.Hidden);
    }

    public static bool ReadIDFile(string path, out string type, out string id)
    {
      type = string.Empty;
      id = string.Empty;
      string filePath = Path.Combine(path, "_id.txt");
      if (!File.Exists(filePath)) return false;
      using (StreamReader sr = new StreamReader(filePath))
      {
        type =  sr.ReadLine().Trim();
        id = sr.ReadToEnd().Trim();
      }
      return true;
    }

    /// <summary>
    /// Remove forbiden chars for path or file names
    /// </summary>
    /// <param name="pathName">a string path</param>
    /// <returns></returns>
    public static string Sanitize(string pathName)
    {
      //Regex from http://stackoverflow.com/questions/62771/how-do-i-check-if-a-given-string-is-a-legal-valid-file-name-under-windows/628
      Regex unspupportedRegex = new Regex("(^(PRN|AUX|NUL|CON|COM[1-9]|LPT[1-9]|(\\.+)$)(\\..*)?$)|(([‌​\\x00-\\x1f\\\\?*:\"‌​;‌​|/<>])+)|([\\. ]+)", RegexOptions.IgnoreCase);
      return unspupportedRegex.Replace(pathName, string.Empty);
    }
  }
}
