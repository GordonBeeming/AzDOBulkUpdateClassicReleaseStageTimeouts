using System.Collections.Generic;

namespace AzDOBulkUpdateClassicReleaseStageTimeouts
{
  public class AppConfig
  {
    public RunModes RunMode { get; set; } = RunModes.ListPipelines;

    public string OrgName { get; set; }
    public string ProjectName { get; set; }
    public string PatKey { get; set; }

    public ListPipelines ListPipelines { get; set; } = new ListPipelines();

    public UpdateTimeoutFor UpdateTimeoutFor { get; set; } = new UpdateTimeoutFor();

  }

  public class ListPipelines
  {
    public string searchText { get; set; }
    public bool searchTextContainsFolderName { get; set; }
    public bool isExactNameMatch { get; set; }
  }

  public class UpdateTimeoutFor
  {
    public int TimeoutInMinutes { get; set; } = 0;

    public List<UpdateTimeoutFor_Definition> Definitions { get; set; } = new List<UpdateTimeoutFor_Definition>();
  }

  public class UpdateTimeoutFor_Definition
  {
    public string FullName { get; set; }
    public List<string> Environments { get; set; } = new List<string>();
  }
}