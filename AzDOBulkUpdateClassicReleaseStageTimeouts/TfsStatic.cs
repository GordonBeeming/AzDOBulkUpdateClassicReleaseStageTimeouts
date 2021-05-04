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
    public const string Standard_Org_UrlFormat = "https://dev.azure.com/{0}";
    public const string Standard_Project_UrlFormat = Standard_Org_UrlFormat + "/{1}";

    public const string ReleaseManager_Org_UrlFormat = "https://vsrm.dev.azure.com/{0}";
    public const string ReleaseManager_Project_UrlFormat = ReleaseManager_Org_UrlFormat + "/{1}";

    #region core

    public const string BOOOOOOOM = "BOOOOOOOM!";
    public static string PatKey { get; set; } = string.Empty;
    public static string OrgName { get; set; } = string.Empty;
    public static string ProjectName { get; set; } = string.Empty;

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
          if (typeof(T) == typeof(string))
          {
            return (T)(object)responseString;
          }
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
          if (typeof(T) == typeof(string))
          {
            requestString = (string)data;
          }
          else
          {
            requestString = JsonConvert.SerializeObject(data);
          }
        }
        return TfsRestTry(uri, () =>
        {
          var responseString = client.UploadString(uri, method, requestString);
          if (typeof(T) == typeof(string))
          {
            return (T)(object)responseString;
          }
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

    private static T Put<T>(string uri, object data, string authHeader)
    {
      return GeneralPushData<T>(uri, data, "PUT", "application/json", authHeader);
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

    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/azure/devops/release/definitions/list?view=azure-devops-rest-6.1
    /// </summary>
    public static DefinitionList GetDefinitions(string searchText, bool searchTextContainsFolderName, bool isExactNameMatch)
    {
      var response = new DefinitionList();
      Get<DefinitionList>(GetUrl(ReleaseManager_Project_UrlFormat, $"/_apis/release/definitions?searchText={searchText}&$expand=environments&isExactNameMatch={isExactNameMatch}&searchTextContainsFolderName={searchTextContainsFolderName}&api-version=6.1-preview.4"), GetAuthorizationHeader(), (data) =>
      {
        response.count += data.count;
        response.value.AddRange(data.value);
        return true;
      });
      return response;
    }

    public static string GetDefinitionAsJson(int id)
    {
      return Get<string>(GetUrl(ReleaseManager_Project_UrlFormat, $"/_apis/release/definitions/{id}?api-version=6.1-preview.4"), GetAuthorizationHeader());
    }

    public static string UpdateDefinitionRawJson(int id, string jsonBody)
    {
      return Put<string>(GetUrl(ReleaseManager_Project_UrlFormat, $"/_apis/release/definitions/{id}?api-version=6.1-preview.4"), jsonBody, GetAuthorizationHeader());
    }

    public static string GetUrl(string urlFormat, string uriRelativeToRoot)
    {
      var baseUri = string.Format(urlFormat, OrgName, ProjectName);
      return $"{baseUri}{uriRelativeToRoot.Replace("//", "/")}";
    }

    public static string GetTeamProjectId()
    {
      var projects = Get<ProjectList>(GetUrl(Standard_Org_UrlFormat, $"/_apis/projects?api-version=2.0"), GetAuthorizationHeader());
      foreach (var item in projects.value)
      {
        if (item.name.Equals(ProjectName, StringComparison.InvariantCultureIgnoreCase))
        {
          return item.id;
        }
      }
      return null;
    }
  }
}
