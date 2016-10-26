using Autodesk.Forge.OAuth;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Autodesk.Forge
{
  public class ForgeApi
  {
    internal const string BASE_URL = "https://developer.api.autodesk.com";

    protected async Task<IRestResponse> MakeRequestAsync(string endPoint, RestSharp.Method method,
      Dictionary<string, string> headers = null, Dictionary<string, string> bodyParameters = null,
      object dataBody = null, String filePath = null)
    {
      var client = new RestClient(BASE_URL);
      var request = new RestRequest(endPoint, method);

      // ToDo: create method overrides

      if (headers != null)
        foreach (KeyValuePair<string, string> item in headers)
          request.AddHeader(item.Key, item.Value);

      if (bodyParameters != null)
        foreach (KeyValuePair<string, string> item in bodyParameters)
          request.AddParameter(item.Key, item.Value);

      if (!string.IsNullOrWhiteSpace(filePath))
      {
        request.AddHeader("Content-Type", MimeType(filePath));
        request.AddHeader("Content-Disposition", string.Format("file; filename=\"{0}\"", Path.GetFileNameWithoutExtension(filePath)));
        request.AddParameter(MimeType(filePath), File.ReadAllBytes(filePath), ParameterType.RequestBody);
        //request.AddFile(Path.GetFileNameWithoutExtension(filePath), filePath);
        //request.AlwaysMultipartFormData = false;
        //request.AddHeader("Content-Type", MimeType(filePath));
      }

      if (dataBody != null)
        // the request.AddJsonBody() override the Content-Type header to RestSharp... this is a workaround
        request.AddParameter(headers["Content-Type"], JsonConvert.SerializeObject(dataBody), ParameterType.RequestBody);

      IRestResponse response = await client.ExecuteTaskAsync(request);

      if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
      {
        OnUnauthorized?.Invoke(this, new StatusCodeEventArgs(endPoint));
        return null;
      }

      return response;
    }

    public string MimeType(string fileName)
    {
      Dictionary<string, string> types = new Dictionary<string, string>();
      types.Add("png", "application/image");
      types.Add("jpg", "application/image");
      types.Add("txt", "application/txt");
      types.Add("ipt", "application/vnd.autodesk.inventor.part");
      types.Add("iam", "application/vnd.autodesk.inventor.assembly");
      types.Add("dwf", "application/vnd.autodesk.autocad.dwf");
      types.Add("dwg", "application/vnd.autodesk.autocad.dwg");
      types.Add("f3d", "application/vnd.autodesk.fusion360");
      types.Add("f2d", "application/vnd.autodesk.fusiondoc");
      types.Add("rvt", "application/vnd.autodesk.revit");
      string extension = Path.GetExtension(fileName).Replace(".", string.Empty);
      return (types.ContainsKey(extension) ? types[extension] : "application/" + extension);
    }
  

    public event EventHandler OnUnauthorized;

    public class StatusCodeEventArgs :  EventArgs
    {
      public StatusCodeEventArgs(string endpoint) { Endpoint = endpoint; }
      public string Endpoint { get; set; }
    }
  }

  public abstract class ApiObject : ForgeApi
  {
    internal Authorization Authorization { get; set; }

    protected ApiObject(Authorization auth)
    {
      Authorization = auth;
    }

    protected async Task<IRestResponse>  CallApi(string endPoint, RestSharp.Method method,
      Dictionary<string, string> headers = null, Dictionary<string, string> bodyParameters = null,
      object dataBody = null, string file = null)
    {
      if (headers == null) headers = new Dictionary<string, string>();
      headers.Add("Authorization", "Bearer " + Authorization.AccessToken);
      return await MakeRequestAsync(endPoint, method, headers, bodyParameters, dataBody, file);
    }
    //virtual public string Name { get; }
  }

  internal interface IIdentifiable
  {
    string ID { get; }
  }

  internal class JsonapiResponse<T>
  {
    public class Jsonapi
    {
      public string version { get; set; }
    }

    public class Links
    {
      public Self self { get; set; }
    }

    public class Self
    {
      public string href { get; set; }
    }

    public Jsonapi jsonapi { get; set; }
    public Links links { get; set; }
    public T data { get; set; }
  }
}
