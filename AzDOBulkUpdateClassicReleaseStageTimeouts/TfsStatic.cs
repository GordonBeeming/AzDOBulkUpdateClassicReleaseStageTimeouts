using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace AzDOBulkUpdateClassicReleaseStageTimeouts
{
#pragma warning disable IDE1006 // Naming Styles

  public static class TfsStatic
  {
    #region core

    public const string BOOOOOOOM = "BOOOOOOOM!";
    public static string PatKey = string.Empty;
    public static string TeamProjectBaseUri = string.Empty;

    private static string GetAuthorizationHeader() => $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($":{PatKey}"))}";

    private static T Get<T>(string uri, string authHeader, Func<T, bool>? onContinuationToken = null, string? continuationToken = null)
    {
      using (var client = new WebClient())
      {
        client.Headers[HttpRequestHeader.Authorization] = authHeader;
        var thisUri = uri;
        if (continuationToken != null)
        {
          thisUri += $"&continuationToken={continuationToken}";
        }
        return TfsRestTry(thisUri, () =>
        {
          var responseString = client.DownloadString(thisUri);
          var response = JsonConvert.DeserializeObject<T>(responseString);
          if (onContinuationToken != null)
          {
            if (client.ResponseHeaders?.AllKeys.Any(o => o.Equals("x-ms-continuationtoken", StringComparison.OrdinalIgnoreCase)) == true)
            {
              continuationToken = client.ResponseHeaders.GetValues("x-ms-continuationtoken")?
                                                        .FirstOrDefault();
            }
            else
            {
              continuationToken = null;
            }
            if (onContinuationToken(response))
            {
              if (continuationToken != null)
              {
                Get<T>(uri, authHeader, onContinuationToken, continuationToken);
              }
            }
          }
          return response;
        });
      }
    }

    private static T GeneralPushData<T>(string uri, object data, string method, string contentType, string authHeader)
    {
      using (var client = new WebClient())
      {
        client.Headers[HttpRequestHeader.Authorization] = authHeader;
        client.Headers[HttpRequestHeader.ContentType] = contentType;
        var requestString = string.Empty;
        if (data != null)
        {
          requestString = JsonConvert.SerializeObject(data);
        }
        return TfsRestTry(uri, () =>
        {
          var responseString = client.UploadString(uri, method, requestString);
          return JsonConvert.DeserializeObject<T>(responseString);
        });
      }
    }

    private static void Post(string uri, object data, string authHeader)
    {
      Post<object>(uri, data, authHeader);
    }

    private static T Post<T>(string uri, object data, string authHeader)
    {
      return GeneralPushData<T>(uri, data, "POST", "application/json", authHeader);
    }

    private static void Patch(string uri, object data, string authHeader)
    {
      Patch<object>(uri, data, authHeader);
    }

    private static T Patch<T>(string uri, object data, string authHeader)
    {
      return GeneralPushData<T>(uri, data, "PATCH", "application/json", authHeader);
    }

    private static void Delete(string uri, object data, string authHeader)
    {
      Delete<object>(uri, data, authHeader);
    }

    private static T Delete<T>(string uri, object data, string authHeader)
    {
      return GeneralPushData<T>(uri, data, "DELETE", "application/json", authHeader);
    }

    private static void Put(string uri, object data, string authHeader)
    {
      GeneralPushData<object>(uri, data, "PUT", "application/json", authHeader);
    }

    private static void Patch2(string uri, object data, string authHeader)
    {
      Patch2<object>(uri, data, authHeader);
    }

    private static T Patch2<T>(string uri, object data, string authHeader)
    {
      return GeneralPushData<T>(uri, data, "PATCH", "application/json-patch+json", authHeader);
    }

    private static T TfsRestTry<T>(string uri, Func<T> f)
    {
      try
      {
        return f();
      }
      catch (WebException webEx) when (webEx.Status == WebExceptionStatus.ProtocolError && (((HttpWebResponse)webEx.Response).StatusCode == HttpStatusCode.BadRequest || ((HttpWebResponse)webEx.Response).StatusCode == HttpStatusCode.NotFound))
      {
        using (var sr = new StreamReader(webEx.Response.GetResponseStream()))
        {
          var responseString = sr.ReadToEnd();
          var exception = JsonConvert.DeserializeObject<RestCallException>(responseString);
          //throw new Exception($"{exception.message} | {uri}");
          if (exception != null)
          {
            throw exception;
          }
          throw webEx;
        }
      }
    }

    public static bool TryCreate(Action get, Action set)
    {
      return TryCreate(() => { get(); return string.Empty; }, set) != null;
    }

    public static string TryCreate(Func<string> get, Action set)
    {
      // this is bad =)
      try
      {
        Console.Write($"creating...");
        string result = get();
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($"exists");
        Console.ForegroundColor = ConsoleColor.White;
        return result;
      }
      catch (Exception exThrow)
      {
        bool throwEx = false;
        try
        {
          set();
        }
        catch (Exception exx) when (exx.Message == TfsStatic.BOOOOOOOM)
        {
          throwEx = true;
        }
        if (throwEx)
        {
          throw exThrow;
        }
        try
        {
          string result = get();
          Console.ForegroundColor = ConsoleColor.DarkGreen;
          Console.WriteLine($"created");
          Console.ForegroundColor = ConsoleColor.White;
          return result;
        }
        catch (Exception ex)
        {
          Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine($"err: {ex.Message}");
          Console.ForegroundColor = ConsoleColor.White;
          return null;
        }
      }
    }

    #endregion

    public static repositories GetGitRepos()
    {
      return Get<repositories>(GetUrl(true, $"/_apis/git/repositories?api-version=1.0"), GetAuthorizationHeader());
    }

    public static CreateServiceEndpointResponse CreateServiceEndpoint(CreateServiceEndpointRequest request)
    {
      return Post<CreateServiceEndpointResponse>(GetUrl(false, $"/_apis/distributedtask/serviceendpoints?api-version=3.0-preview.1"), request, GetAuthorizationHeader());
    }

    public static CreateRepoResponse CreateRepo(CreateRepoRequest request)
    {
      return Post<CreateRepoResponse>(GetUrl(true, $"/_apis/git/repositories?api-version=1.0"), request, GetAuthorizationHeader());
    }

    public static CreateImportRequestResponse CreateImportRequest(string repoName, CreateImportRequestRequest request)
    {
      return Post<CreateImportRequestResponse>(GetUrl(false, $"/_apis/git/repositories/{repoName}/importRequests?api-version=5.0-preview.1"), request, GetAuthorizationHeader());
    }

    #region Dashboards

    public static DashboardsList GetDashboards(string teamName, bool projectDashboard)
    {
      var teamPart = projectDashboard ? string.Empty : $"/{teamName}";
      return Get<DashboardsList>(GetUrl(false, $"{teamPart}/_apis/dashboard/dashboards?api-version=6.0-preview.3"), GetAuthorizationHeader());
    }

    public static DashboardInfo GetDashboard(string teamName, bool projectDashboard, string dashboardId)
    {
      var teamPart = projectDashboard ? string.Empty : $"/{teamName}";
      return Get<DashboardInfo>(GetUrl(false, $"{teamPart}/_apis/dashboard/dashboards/{dashboardId}?api-version=6.0-preview.3"), GetAuthorizationHeader());
    }

    public static DashboardInfo CreateDashboard(string teamName, bool projectDashboard, DashboardInfo dashboardData)
    {
      var teamPart = projectDashboard ? string.Empty : $"/{teamName}";
      return Post<DashboardInfo>(GetUrl(false, $"{teamPart}/_apis/dashboard/dashboards?api-version=6.0-preview.3"), dashboardData, GetAuthorizationHeader());
    }

    public static void DeleteDashboard(string teamName, bool projectDashboard, string dashboardId)
    {
      var teamPart = projectDashboard ? string.Empty : $"/{teamName}";
      Delete(GetUrl(false, $"{teamPart}/_apis/dashboard/dashboards/{dashboardId}?api-version=6.0-preview.3"), null, GetAuthorizationHeader());
    }

    #endregion

    #region Work Item Queries

    public static WorkItemQueries GetWorkItemQueries()
    {
      return Get<WorkItemQueries>(GetUrl(false, $"/_apis/wit/queries?api-version=6.0"), GetAuthorizationHeader());
    }

    public static WorkItemQuery GetWorkItemQuery(string queryPath, QueryExpand queryExpand = QueryExpand.none, int depth = 2)
    {
      return Get<WorkItemQuery>(GetUrl(false, $"/_apis/wit/queries/{queryPath}?api-version=6.0&$expand={queryExpand}&$depth={depth}"), GetAuthorizationHeader());
    }

    public static WorkItemQuery CreateWorkItemQueryFolder(string queryPath, string folderName)
    {
      return Post<WorkItemQuery>(GetUrl(false, $"/_apis/wit/queries/{queryPath}?api-version=6.0"), new CreateWorkItemQueryFolderRequest { name = folderName }, GetAuthorizationHeader());
    }

    public static WorkItemQuery CreateWorkItemQuery(string queryPath, WorkItemQuery workItemQuery)
    {
      workItemQuery.id = Guid.Empty.ToString();
      return Post<WorkItemQuery>(GetUrl(false, $"/_apis/wit/queries/{queryPath}?api-version=6.0"), workItemQuery, GetAuthorizationHeader());
    }

    public static WorkItemQuery UpdateWorkItemQuery(WorkItemQuery workItemQuery)
    {
      workItemQuery.path += $"/{workItemQuery.name}";
      return Patch<WorkItemQuery>(GetUrl(false, $"/_apis/wit/queries/{workItemQuery.id}?api-version=6.0"), workItemQuery, GetAuthorizationHeader());
    }

    //public static void DeleteDashboard(string teamName, string dashboardId)
    //{
    //  Delete(GetUrl( false, $"/{teamName}/_apis/dashboard/dashboards/{dashboardId}?api-version=6.0-preview.3"), null, GetAuthorizationHeader());
    //}

    #endregion

    #region Builds

    public static BuildList GetBuilds()
    {
      return Get<BuildList>(GetUrl(false, $"/_apis/build/builds?api-version=6.0"), GetAuthorizationHeader());
    }

    #endregion

    #region Teams

    public static TeamList GetTeams(string projectName)
    {
      var response = new TeamList();
      Get<TeamList>(GetUrl(true, $"/_apis/projects/{projectName}/teams?api-version=6.0&$top=9999"), GetAuthorizationHeader(), (data) =>
     {
       response.count += data.count;
       response.value.AddRange(data.value);
       return true;
     });
      return response;
    }

    public static TeamIterationList GetTeamIterations(string teamName, bool currentOnly = false)
    {
      var currentOnlyFilter = currentOnly ? "&$timeframe=current" : string.Empty;
      var response = new TeamIterationList();
      Get<TeamIterationList>(GetUrl(false, $"/{teamName}/_apis/work/teamsettings/iterations?api-version=6.0{currentOnlyFilter}"), GetAuthorizationHeader(), (data) =>
     {
       response.count += data.count;
       response.value.AddRange(data.value);
       return true;
     });
      return response;
    }

    #endregion

    #region Team Boards

    public static TeamBoardList GetTeamBoards(string teamName)
    {
      var response = new TeamBoardList();
      Get<TeamBoardList>(GetUrl(false, $"/{teamName}/_apis/work/boards?api-version=6.0"), GetAuthorizationHeader(), (data) =>
     {
       response.count += data.count;
       response.value.AddRange(data.value);
       return true;
     });
      return response;
    }

    public static TeamBoard GetTeamBoard(string teamName, string boardId)
    {
      return Get<TeamBoard>(GetUrl(false, $"/{teamName}/_apis/work/boards/{boardId}?api-version=6.0"), GetAuthorizationHeader());
    }

    #endregion

    public static string GetUrl(bool excludeProject, string uriRelativeToRoot)
    {
      var baseUri = TeamProjectBaseUri;
      if (excludeProject)
      {
        baseUri = baseUri.Remove(baseUri.LastIndexOf('/'));
      }
      return $"{baseUri}{uriRelativeToRoot.Replace("//", "/")}";
    }

    public static string GetTeamProjectId()
    {
      var teamProjectName = GetTeamProjectName();
      var projects = Get<GetProjects>(GetUrl(true, $"/_apis/projects?api-version=2.0"), GetAuthorizationHeader());
      foreach (var item in projects.value)
      {
        if (item.name.Equals(teamProjectName, StringComparison.InvariantCultureIgnoreCase))
        {
          return item.id;
        }
      }
      return null;
    }

    public static string GetTeamProjectName()
    {
      var baseUri = TeamProjectBaseUri;
      var teamProjectName = baseUri.Remove(0, baseUri.LastIndexOf('/') + 1);
      return teamProjectName;
    }
  }
}
