using System;
using System.Collections.Generic;

namespace AzDOBulkUpdateClassicReleaseStageTimeouts
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
































  public class DefinitionList
  {
    public int count { get; set; }
    public List<DefinitionItem> value { get; set; } = new List<DefinitionItem>();
  }

  public class DefinitionItem
  {
    public int id { get; set; }
    public string name { get; set; }
    public string path { get; set; }
    public List<DefinitionEnvironment> environments { get; set; } = new List<DefinitionEnvironment>();
  }

  public class DefinitionEnvironment
  {
    public int id { get; set; }
    public string name { get; set; }
    public int rank { get; set; }
  }
































  public class ProjectList
  {
    public int count { get; set; }
    public ProjectItem[] value { get; set; }
  }

  public class ProjectItem
  {
    public string id { get; set; }
    public string name { get; set; }
    public string url { get; set; }
    public string state { get; set; }
    public int revision { get; set; }
    public string visibility { get; set; }
    public DateTime lastUpdateTime { get; set; }
  }
}
