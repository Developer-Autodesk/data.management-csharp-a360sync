using Autodesk.Forge.OAuth;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace Autodesk.Forge
{
  public static class PredefinedHeadersExtension
  {
    public enum PredefinedHeaders
    {
      /// <summary>
      /// Content-Type: application/vnd.api+json
      /// </summary>
      ContentTypeJson,
      /// <summary>
      /// Content-Type: application/x-www-form-urlencoded
      /// </summary>
      ContentTypeFormUrlEncoded,
      /// <summary>
      /// Accept: application/vnd.api+json
      /// </summary>
      AcceptJson

    }

    public static void AddHeader(this Dictionary<string, string> obj, PredefinedHeaders header)
    {
      switch(header)
      {
        case PredefinedHeaders.ContentTypeJson:
          obj.Add("Content-Type", "application/vnd.api+json");
          break;
        case PredefinedHeaders.AcceptJson:
          obj.Add("Accept", "application/vnd.api+json");
          break;
        case PredefinedHeaders.ContentTypeFormUrlEncoded:
          obj.Add("Content-Type", "application/x-www-form-urlencoded");
          break;
      }
    }
  }

  public class Rest
  {
    internal const string BASE_URL = "https://developer.api.autodesk.com";

    protected IRestResponse MakeRequest(string endPoint, RestSharp.Method method,
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

      IRestResponse response = client.Execute(request);

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
  

    public static event EventHandler OnUnauthorized;

    public class StatusCodeEventArgs :  EventArgs
    {
      public StatusCodeEventArgs(string endpoint) { Endpoint = endpoint; }
      public string Endpoint { get; set; }
    }
  }

  public abstract class ApiObject : Rest
  {
    internal Authorization Authorization { get; set; }

    protected ApiObject(Authorization auth)
    {
      Authorization = auth;
    }

    protected IRestResponse CallApi(string endPoint, RestSharp.Method method,
      Dictionary<string, string> headers = null, Dictionary<string, string> bodyParameters = null,
      object dataBody = null, string file = null)
    {
      if (headers == null) headers = new Dictionary<string, string>();
      headers.Add("Authorization", "Bearer " + Authorization.AccessToken);
      return base.MakeRequest(endPoint, method, headers, bodyParameters, dataBody, file);
    }

    //virtual public string ID { get; }
    //virtual public string Name { get; }
  }

  public class JsonapiResponse<T>
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
