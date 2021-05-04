using Newtonsoft.Json;
using System;
using System.IO;

namespace AzDOBulkUpdateClassicReleaseStageTimeouts
{
  class Program
  {
    static AppConfig _config = null;

    static void Main(string[] args)
    {
      if (!LoadConfig(args))
      {
        return;
      }

      MigrateDashboard();

      DoneDone();
    }

    private static void MigrateDashboard()
    {
    }

    #region fluff

    private static bool LoadConfig(string[] args)
    {
      var fileName = "config.json";
      if (args != null && args.Length > 0)
      {
        fileName = args[0].Trim('\"').Trim('\'');
      }
      SetConsoleThings();
      if (!File.Exists($".\\{fileName}"))
      {
        WriteLine($"{fileName} is missing!", ConsoleColor.Red);
        return false;
      }
      var json = File.ReadAllText($".\\{fileName}");
      try
      {
        _config = JsonConvert.DeserializeObject<AppConfig>(json);
      }
      catch
      {
        WriteLine(json);
        WriteLine();
        WriteLine();
        throw;
      }

      TfsStatic.TeamProjectBaseUri = _config.SourceTeamProjectBaseUri;
      TfsStatic.PatKey = _config.SourcePatKey;

      if (!GetPatToken())
      {
        return false;
      }

      return true;
    }

    private static bool GetPatToken()
    {
      Console.WriteLine("PAT keys can be generated in TFS, keep this safe. With this key we are able to impersonate you using the TFS API's.");
      Console.WriteLine("Steps to create: https://www.visualstudio.com/en-us/docs/setup-admin/team-services/use-personal-access-tokens-to-authenticate");
      Console.WriteLine("TFS Uri: https://{account}/{tpc}/_details/security/tokens");
      Console.WriteLine();
      if (string.IsNullOrEmpty(TfsStatic.PatKey))
      {
        Console.WriteLine($"Source: {TfsStatic.TeamProjectBaseUri}");
        Console.Write("Enter you Source PAT key: ");
        TfsStatic.PatKey = Console.ReadLine()!;
        if ((TfsStatic.PatKey?.Trim() ?? string.Empty).Length == 0)
        {
          Console.WriteLine();
          Console.WriteLine("Seems you didn't supply a key.");
          Console.ReadLine();
          return false;
        }
      }
      Console.Clear();
      return true;
    }

    private static void SetConsoleThings()
    {
      Console.ForegroundColor = ConsoleColor.White;
      Console.BackgroundColor = ConsoleColor.Black;
      Console.Clear();
    }

    private static void Write(string message = "", ConsoleColor colour = ConsoleColor.White)
    {
      Console.ForegroundColor = colour;
      Console.Write(message);
      Console.ForegroundColor = ConsoleColor.White;
    }

    private static void WriteLine(string message = "", ConsoleColor colour = ConsoleColor.White)
    {
      Console.ForegroundColor = colour;
      Console.WriteLine(message);
      Console.ForegroundColor = ConsoleColor.White;
    }

    private static void DoneDone()
    {
      WriteLine();
      WriteLine();
      WriteLine();
      WriteLine("Done!");
      //Console.ReadLine();
    }

    internal static void WriteFileProgress(string message)
    {
      try
      {
        File.AppendAllText(".\\progress.log", $"[{DateTime.UtcNow:yyyyMMdd HHmm}] {message}{Environment.NewLine}");
      }
      catch { }
    }

    #endregion
  }
}
