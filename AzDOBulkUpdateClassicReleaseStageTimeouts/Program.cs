using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

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

      switch (_config.RunMode)
      {
        case RunModes.ListPipelines:
          ListPipelines();
          break;
        case RunModes.UpdateTimeoutFor:
          UpdateTimeoutFor();
          break;
      }

      DoneDone();
    }

    private static void ListPipelines()
    {
      foreach (var definition in TfsStatic.GetDefinitions(_config.ListPipelines.searchText, _config.ListPipelines.searchTextContainsFolderName, _config.ListPipelines.isExactNameMatch).value)
      {
        WriteLine($@"
{{
  ""FullName"": ""{GetDefinitionFullName(definition, true)}"", // {TfsStatic.GetUrl(TfsStatic.Standard_Project_UrlFormat, $"/_release?_a=releases&view=mine&definitionId={definition.id}")}
  ""Environments"": [
	{string.Join($",{Environment.NewLine}", definition.environments.OrderBy(o => o.rank).Select(o => $"\"{o.name.Replace("\\", "\\\\")}\""))}
  ]
}}
");
      }
    }

    private static string GetDefinitionFullName(DefinitionItem definition, bool forJson = false) => $"{(forJson ? definition.path.Replace("\\", "\\\\") : definition.path)}{definition.name}";

    private static void UpdateTimeoutFor()
    {
      var allDefinitions = TfsStatic.GetDefinitions(string.Empty, false, false).value;
      foreach (var definition in _config.UpdateTimeoutFor.Definitions)
      {
        WriteLine($"{definition.FullName}");
        var defRef = allDefinitions.FirstOrDefault(o => GetDefinitionFullName(o).Equals(definition.FullName, StringComparison.OrdinalIgnoreCase));
        if (defRef == null)
        {
          WriteLine($"'{definition.FullName}' missing.");
          continue;
        }
        var fullDefJson = TfsStatic.GetDefinitionAsJson(defRef.id);
        var root = JObject.Parse(fullDefJson);

        var environments = root["environments"];
        if (environments == null)
        {
          WriteLine($"environments node missing.");
          continue;
        }
        foreach (var environment in environments.Children())
        {
          var environmentName = environment.Value<string>("name");
          Write($"- {environmentName}...");
          if (!definition.Environments.Any(o => o.Equals(environmentName, StringComparison.OrdinalIgnoreCase)))
          {
            WriteLine($"skipped", ConsoleColor.DarkGray);
          }
          var deployPhases = environment["deployPhases"] as JArray;
          if (deployPhases == null)
          {
            WriteLine($"deployPhases node missing.");
            continue;
          }
          for (int i = 0; i < deployPhases.Count; i++)
          {
            var deployPhase = deployPhases[i];
            var deploymentInput = deployPhase["deploymentInput"];
            if (deploymentInput == null)
            {
              WriteLine($"deploymentInput node missing on environments.deployPhases[{i}].");
              continue;
            }
            var timeoutInMinutes = deploymentInput["timeoutInMinutes"] as JValue;
            if (timeoutInMinutes == null)
            {
              WriteLine($"timeoutInMinutes node missing on environments.deployPhases[{i}].deploymentInput.");
              continue;
            }
            timeoutInMinutes.Value = _config.UpdateTimeoutFor.TimeoutInMinutes;
          }
        }

        TfsStatic.UpdateDefinitionRawJson(defRef.id, root.ToString());
        WriteLine($"updated", ConsoleColor.Green);
      }
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

      if (_config.RunMode == RunModes.ShowOptions)
      {
        WriteLine($"Run Mode Options:");
        foreach (var runMode in Enum.GetNames(typeof(RunModes)))
        {
          WriteLine($"- {runMode}");
        }
        return false;
      }

      TfsStatic.OrgName = _config.OrgName;
      TfsStatic.ProjectName = _config.ProjectName;
      TfsStatic.PatKey = _config.PatKey;

      if (!GetPatToken())
      {
        return false;
      }

      return true;
    }

    private static bool GetPatToken()
    {
      Console.WriteLine("PAT keys can be generated in Azure DevOps, keep this safe. With this key we are able to impersonate you using the TFS API's.");
      Console.WriteLine("Steps to create: https://www.visualstudio.com/en-us/docs/setup-admin/team-services/use-personal-access-tokens-to-authenticate");
      Console.WriteLine($"Uri: https://dev.azure.com/{_config.OrgName}/_usersSettings/tokens");
      Console.WriteLine();
      if (string.IsNullOrEmpty(TfsStatic.PatKey))
      {
        Console.Write("Enter you PAT key: ");
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
