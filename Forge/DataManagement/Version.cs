using Autodesk.Forge.OAuth;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autodesk.Forge.DataManagement
{
  public class VersionsCollection : ApiObject//, IEnumerable<Item>
  {
    private Item Owner { get; set; }

    internal VersionsCollection(Item owner, Authorization auth) : base(auth)
    {
      Owner = owner;
    }

    public void Add(Storage.StorageResponse storage)
    {
      Version.VersionRequest newVersion = new DataManagement.Version.VersionRequest(Owner.Json.attributes.displayName, storage.id, Owner.Json.id);
      Dictionary<string, string> headers = new Dictionary<string, string>();
      headers.AddHeader(PredefinedHeadersExtension.PredefinedHeaders.ContentTypeJson);
      headers.AddHeader(PredefinedHeadersExtension.PredefinedHeaders.AcceptJson);
      IRestResponse response = CallApi(string.Format("/data/v1/projects/{0}/versions", Owner.Owner.Owner.Json.id), Method.POST, headers, null, newVersion, null);
      //Json = JsonConvert.DeserializeObject<Version.>(response.Content).data;
    }
  }

  public class Version : ApiObject
  {
    internal Version(Authorization auth) : base(auth)
    {

    }

    #region Version Request Data Model Structure

    public class VersionRequest
    {
      public VersionRequest(string fileName, string storageId, string itemId)
      {
        jsonapi = new Jsonapi
        {
          version = "1.0"
        };

        data = new Data
        {
          type = "versions",
          attributes = new Data.Attributes
          {
            name = fileName,
            extension = new Data.Attributes.Extension
            {
              type = "versions:autodesk.core:File",
              version = "1.0"
            }
          },
          relationships = new Data.Relationships
          {
            item = new Data.Relationships.Item
            {
              data = new Data.Relationships.Item.Data
              {
                type = "items",
                id = itemId
              }
            },
            storage = new Data.Relationships.Storage
            {
              data = new Data.Relationships.Storage.Data
              {
                type = "objects",
                id = storageId
              }
            }
          }
        };
      }

      public Jsonapi jsonapi { get; set; }
      public Data data { get; set; }

      public class Jsonapi
      {
        public string version { get; set; }
      }

      public class Data
      {
        public string type { get; set; }
        public Attributes attributes { get; set; }
        public Relationships relationships { get; set; }

        public class Attributes
        {
          public string name { get; set; }
          public Extension extension { get; set; }
          public class Extension
          {
            public string type { get; set; }
            public string version { get; set; }
          }
        }

        public class Relationships
        {
          public Item item { get; set; }
          public Storage storage { get; set; }

          public class Item
          {
            public Data data { get; set; }
            public class Data
            {
              public string type { get; set; }
              public string id { get; set; }
            }
          }

          public class Storage
          {
            public Data data { get; set; }
            public class Data
            {
              public string type { get; set; }
              public string id { get; set; }
            }
          }
        }
      }
    }

    #endregion  
  }
}
