using Autodesk.Forge.OAuth;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autodesk.Forge.OSS
{
  class Bucket : ApiObject
  {
    public Bucket(Authorization oauth) : base(oauth)
    {
      
    }

    public async Task<BucketResponse> UploadFile(string filePath, string bucketKey, string objectName)
    {
      IRestResponse response = await CallApi(string.Format("oss/v2/buckets/{0}/objects/{1}", bucketKey, objectName), RestSharp.Method.PUT, null, null, null, filePath );
      return JsonConvert.DeserializeObject<JsonapiResponse<Bucket.BucketResponse>>(response.Content).data;
    }

    public static bool Extract(string id, out string bucketKey, out string objectName)
    {
      try
      {
        objectName = id.Split('/')[1];
        bucketKey = id.Split('/')[0].Split(':').Last<string>();
        return true;
      }
      catch
      {
        bucketKey = string.Empty;
        objectName = string.Empty;
        return false;
      }
    }

    public class BucketResponse
    {
      public string bucketKey { get; set; }
      public string objectId { get; set; }
      public string objectKey { get; set; }
      public string sha1 { get; set; }
      public int size { get; set; }
      public string contentType { get; set; }
      public string location { get; set; }
    }
  }
}
