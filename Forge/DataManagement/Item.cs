using Autodesk.Forge.OAuth;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Autodesk.Forge.DataManagement
{
  public class ItemsCollection : ApiObject, IEnumerable<Item>
  {
    private Folder Owner { get; set; }

    internal ItemsCollection(Folder owner, Authorization auth) : base(auth)
    {
      Owner = owner;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return Enumerator();
    }

    IEnumerator<Item> IEnumerable<Item>.GetEnumerator()
    {
      return Enumerator();
    }

    /// <summary>
    /// Return a item with same displayName
    /// </summary>
    /// <param name="displayName">The display name (e.g. MyFile.dwg)</param>
    /// <returns></returns>
    public Item Contains(string displayName)
    {
      foreach (Item i in this)
        if (i.Json.attributes.displayName.Equals(displayName))
          return i;
      return null;
    }

    private IList<Item> _items = null;

    internal void Invalidate()
    {
      _items = null;
    }

    private IEnumerator<Item> Enumerator()
    {
      if (_items == null)
      {
        _items = new List<Item>();
        IRestResponse response = CallApi(string.Format("data/v1/projects/{0}/folders/{1}/contents", Owner.Owner.Json.id, System.Uri.EscapeUriString(Owner.Json.id)), Method.GET);
        IList<Item.ItemResponse.Data> itemsJsonData = JsonConvert.DeserializeObject<JsonapiResponse<IList<Item.ItemResponse.Data>>>(response.Content).data;
        foreach (Item.ItemResponse.Data itemJsonData in itemsJsonData)
        {
          Item item = new Item(Authorization);
          item.Json = itemJsonData;
          item.Owner = Owner;
          _items.Add(item);
        }
      }
      return _items.GetEnumerator();
    }
  }

  public class Item : ApiObject
  {
    internal Folder Owner { get; set; }

    internal Item(Authorization auth) : base(auth) { }

    internal Item(string fileName, Storage.StorageResponse storage, int version, Folder parentFolder) : base(parentFolder.Authorization)
    {
      Owner = parentFolder;
      Item.ItemRequest newItem = new DataManagement.Item.ItemRequest(fileName, storage.id, 1, parentFolder);
      Dictionary<string, string> headers = new Dictionary<string, string>();
      headers.AddHeader(PredefinedHeadersExtension.PredefinedHeaders.ContentTypeJson);
      headers.AddHeader(PredefinedHeadersExtension.PredefinedHeaders.AcceptJson);
      IRestResponse response = CallApi(string.Format("data/v1/projects/{0}/items", parentFolder.Owner.Json.id), Method.POST, headers, null, newItem, null);
      Json = JsonConvert.DeserializeObject<Item.ItemResponse>(response.Content).data;
      Owner.Contents.Items.Invalidate(); // need to force this list to rebuild
    }

    private VersionsCollection _versions = null;

    public VersionsCollection Versions
    {
      get
      {
        if (_versions == null)
          _versions = new VersionsCollection(this, Authorization);
        return _versions;
      }
    }

    public ItemResponse.Data Json { get; set; }

    #region Item Request Data Model Structure"

    public class ItemRequest
    {
      public ItemRequest(string fileName, string itemId, int version, Folder parentFolder)
      {
        jsonapi = new Jsonapi
        {
          version = "1.0"
        };

        data = new Data
        {
          type = "items",
          attributes = new Data.Attributes
          {
            displayName = fileName,
            extension = new Data.Attributes.Extension
            {
              type = "items:autodesk.core:File",
              version = "1.0"
            }
          },
          relationships = new Data.Relationships
          {
            tip = new Data.Relationships.Tip
            {
              data = new Data.Relationships.Tip.Data
              {
                type = "versions",
                id = version.ToString()
              }
            },
            parent = new Data.Relationships.Parent
            {
              data = new Data.Relationships.Parent.Data
              {
                type = "folders",
                id = parentFolder.Json.id
              }
            }
          }
        };
        included = new List<Included>() {
          new Included
          {
            type = "versions",
            id = version.ToString(),
            attributes = new Included.Attributes
            {
              name = fileName,
              extension = new Included.Attributes.Extension
              {
                type = "versions:autodesk.core:File",
                version = "1.0"
              }
            },
            relationships = new Included.Relationships
            {
              storage = new Included.Relationships.Storage
              {
                data = new Included.Relationships.Storage.Data
                {
                  type = "objects",
                  id = itemId
                }
              }
            }
          }
        };
      }


      public Jsonapi jsonapi { get; set; }
      public Data data { get; set; }
      public List<Included> included { get; set; }

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
          public string displayName { get; set; }
          public Extension extension { get; set; }

          public class Extension
          {
            public string type { get; set; }
            public string version { get; set; }
          }
        }
        public class Relationships
        {
          public Tip tip { get; set; }
          public Parent parent { get; set; }

          public class Tip
          {
            public Data data { get; set; }

            public class Data
            {
              public string type { get; set; }
              public string id { get; set; }
            }
          }
          public class Parent
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
      public class Included
      {
        public string type { get; set; }
        public string id { get; set; }
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
          public Storage storage { get; set; }
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

    #region Item Response Data Model Structure

    public class ItemResponse
    {
      public Jsonapi jsonapi { get; set; }
      public Links links { get; set; }
      public Data data { get; set; }
      public List<Included> included { get; set; }

      public class Jsonapi
      {
        public string version { get; set; }
      }

      public class Links
      {
        public Self self { get; set; }
        public class Self
        {
          public string href { get; set; }
        }
      }

      public class Data
      {
        public string type { get; set; }
        public string id { get; set; }
        public Attributes attributes { get; set; }
        public Links links { get; set; }
        public Relationships relationships { get; set; }

        public class Attributes
        {
          public string displayName { get; set; }
          public string createTime { get; set; }
          public string createUserId { get; set; }
          public string lastModifiedTime { get; set; }
          public string lastModifiedUserId { get; set; }
          public Extension extension { get; set; }

          public class Extension
          {
            public string type { get; set; }
            public string version { get; set; }
            public Schema schema { get; set; }
            public Data2 data { get; set; }

            public class Schema
            {
              public string href { get; set; }
            }

            public class Data2
            {
            }
          }
        }

        public class Links
        {
          public Self self { get; set; }
          public class Self
          {
            public string href { get; set; }
          }
        }

        public class Relationships
        {
          public Tip tip { get; set; }
          public Versions versions { get; set; }
          public Parent parent { get; set; }
          public Refs refs { get; set; }
          public Links links { get; set; }

          public class Tip
          {
            public Data data { get; set; }
            public Links links { get; set; }
            public class Data
            {
              public string type { get; set; }
              public string id { get; set; }
            }

            public class Links
            {
              public Related related { get; set; }
              public class Related
              {
                public string href { get; set; }
              }
            }
          }

          public class Versions
          {
            public Links links { get; set; }
            public class Links
            {
              public Related related { get; set; }
              public class Related
              {
                public string href { get; set; }
              }
            }
          }

          public class Parent
          {
            public Data data { get; set; }
            public Links links { get; set; }

            public class Data
            {
              public string type { get; set; }
              public string id { get; set; }
            }
            public class Links
            {
              public Related related { get; set; }
              public class Related
              {
                public string href { get; set; }
              }
            }
          }

          public class Refs
          {
            public Links links { get; set; }
            public class Links
            {
              public Self self { get; set; }
              public Related related { get; set; }

              public class Self
              {
                public string href { get; set; }
              }

              public class Related
              {
                public string href { get; set; }
              }
            }
          }

          public class Links
          {
            public Links1 links { get; set; }
            public class Links1
            {
              public Self self { get; set; }
              public class Self
              {
                public string href { get; set; }
              }
            }
          }
        }
      }

      public class Included
      {
        public string type { get; set; }
        public string id { get; set; }
        public Attributes2 attributes { get; set; }
        public Links9 links { get; set; }
        public Relationships2 relationships { get; set; }


        public class Attributes2
        {
          public string name { get; set; }
          public string displayName { get; set; }
          public string createTime { get; set; }
          public string createUserId { get; set; }
          public string lastModifiedTime { get; set; }
          public string lastModifiedUserId { get; set; }
          public int versionNumber { get; set; }
          public string mimeType { get; set; }
          public string fileType { get; set; }
          public int storageSize { get; set; }
          public Extension2 extension { get; set; }

          public class Extension2
          {
            public string type { get; set; }
            public string version { get; set; }
            public Schema2 schema { get; set; }
            public Data5 data { get; set; }

            public class Schema2
            {
              public string href { get; set; }
            }

            public class Data5
            {
            }
          }
        }

        public class Links9
        {
          public Self5 self { get; set; }
          public class Self5
          {
            public string href { get; set; }
          }
        }

        public class Relationships2
        {
          public Item item { get; set; }
          public Links11 links { get; set; }
          public Refs2 refs { get; set; }
          public DownloadFormats downloadFormats { get; set; }
          public Derivatives derivatives { get; set; }
          public Thumbnails thumbnails { get; set; }
          public Storage storage { get; set; }

          public class Item
          {
            public Data6 data { get; set; }
            public Links10 links { get; set; }
            public class Data6
            {
              public string type { get; set; }
              public string id { get; set; }
            }

            public class Links10
            {
              public Related5 related { get; set; }
              public class Related5
              {
                public string href { get; set; }
              }
            }
          }

          public class Links11
          {
            public Links12 links { get; set; }
            public class Links12
            {
              public Self6 self { get; set; }
              public class Self6
              {
                public string href { get; set; }
              }
            }
          }

          public class Refs2
          {
            public Links13 links { get; set; }
            public class Links13
            {
              public Self7 self { get; set; }
              public Related6 related { get; set; }
              public class Self7
              {
                public string href { get; set; }
              }
              public class Related6
              {
                public string href { get; set; }
              }
            }
          }

          public class DownloadFormats
          {
            public Links14 links { get; set; }
            public class Links14
            {
              public Related7 related { get; set; }
              public class Related7
              {
                public string href { get; set; }
              }
            }
          }

          public class Derivatives
          {
            public Data7 data { get; set; }
            public Meta meta { get; set; }
            public class Data7
            {
              public string type { get; set; }
              public string id { get; set; }
            }
            public class Meta
            {
              public Link link { get; set; }
              public class Link
              {
                public string href { get; set; }
              }
            }
          }

          public class Thumbnails
          {
            public Data8 data { get; set; }
            public Meta2 meta { get; set; }
            public class Data8
            {
              public string type { get; set; }
              public string id { get; set; }
            }
            public class Meta2
            {
              public Link2 link { get; set; }
              public class Link2
              {
                public string href { get; set; }
              }
            }
          }

          public class Storage
          {
            public Data9 data { get; set; }
            public Meta3 meta { get; set; }
            public class Data9
            {
              public string type { get; set; }
              public string id { get; set; }
            }
            public class Meta3
            {
              public Link3 link { get; set; }
              public class Link3
              {
                public string href { get; set; }
              }
            }
          }
        }
      }
    }

    #endregion
  }
}
