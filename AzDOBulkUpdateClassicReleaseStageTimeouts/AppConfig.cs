namespace AzDOBulkUpdateClassicReleaseStageTimeouts
{
  public class AppConfig
  {
    public string SourceTeamProjectBaseUri { get; set; }
    public string TargetTeamProjectBaseUri { get; set; }
    public string SourcePatKey { get; set; }
    public string TargetPatKey { get; set; }

    public string SourceTeamName { get; set; }
    public string SourceDashboardName { get; set; }
    public bool SourceAsProject { get; set; }

    public string TargetTeamName { get; set; }
    public string TargetDashboardName { get; set; }
    public bool TargetAsProject { get; set; }
  }

}
