using System;

namespace AzDOBulkUpdateClassicReleaseStageTimeouts
{
  public class RestCallException : Exception
  {
    public string id { get; set; }
    public object innerException { get; set; }
    public string message { get; set; }
    public string typeName { get; set; }
    public string typeKey { get; set; }
    public int errorCode { get; set; }
    public int eventId { get; set; }

    public override string Message => message;
  }
}
