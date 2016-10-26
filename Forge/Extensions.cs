using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autodesk.Forge.Extensions
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
      switch (header)
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
}
