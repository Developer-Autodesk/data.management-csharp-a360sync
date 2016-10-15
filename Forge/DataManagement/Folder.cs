using Autodesk.Forge.OAuth;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Autodesk.Forge.DataManagement
{
  public class FolderCollection : ApiObject, IEnumerable<Project>
  {
    internal FolderCollection(Authorization accessToken) : base(accessToken)
    {
      throw new NotImplementedException();
    }

    public IEnumerator<Project> GetEnumerator()
    {
      throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      throw new NotImplementedException();
    }
  }

  public class Folder : ApiObject
  {
    private Project _owner = null;
    internal Project Owner
    {
      get
      {
        if (_owner == null)
          _owner = new Project(this.Json.relationships.parent.data.id, Authorization);
        return _owner;
      }
      set { _owner = value; }
    }

    internal Folder(Project owner, string folderId) : base(owner.Authorization)
    {
      Owner = owner;
      Init(owner.Json.id, folderId);
    }

    /*public Folder(string projectId, string folderId, Authorization auth) : base(auth)
    {
      Init(projectId, folderId);
    }*/

    private void Init(string projectId, string folderId)
    {
      IRestResponse response = CallApi(string.Format("data/v1/projects/{0}/folders/{1}", projectId, folderId), Method.GET);
      FolderResponse folderJsonData = JsonConvert.DeserializeObject<JsonapiResponse<FolderResponse>>(response.Content).data;
      this.Json = folderJsonData;
    }

    private FolderContents _contents;
    public FolderContents Contents
    {
      get
      {
        if (_contents == null)
          _contents = new FolderContents(this, this.Authorization);
        return _contents;
      }
    }

    public class FolderContents : ApiObject
    {
      private Folder Owner { get; set; }
      internal FolderContents(Folder owner, Authorization auth): base(auth)
      {
        Owner = owner;
      }

      private ItemsCollection _items;
      public ItemsCollection Items
      {
        get
        {
          if (_items == null)
            _items = new ItemsCollection(Owner, this.Authorization);
          return _items;
        }
      }
      
      //public FolderCollection Folders { get; } // ToDo
    }

    //public override string ID { get { return Json.id; } }
    //public override string Name { get { return Json.attributes.name; } }

    /// <summary>
    /// Upload a files to the folder
    /// </summary>
    /// <param name="filePath">Full path of the file</param>
    /// <param name="createNewVersion">If file already on the folder, TRUE will create new version, FALSE will do nothing</param>
    /// <returns>The newly created item</returns>
    public Item UploadFile(string filePath, bool createNewVersion)
    {
      // ToDo: check if file exists (then create new version)

      // Following this tutorial:
      // https://developer.autodesk.com/en/docs/data/v2/tutorials/upload-file/

      // Step 1: Find the hub that has your resource
      // no need to navigate throuhg Hubs for this implementattion

      // Step 2: Find the project that has your resource
      // .Owner property

      //Step 3: Create a storage location
      string fileName = Path.GetFileName(filePath);
      Storage.StorageResponse storageDef = CreateStorage(fileName);

      // Step 4: Upload a file to the storage location
      OSS.Bucket bucket = new OSS.Bucket(Authorization);
      string bucketKey, objectName;
      OSS.Bucket.Extract(storageDef.id, out bucketKey, out objectName);
      bucket.UploadFile(filePath, bucketKey, objectName);

      var id = storageDef.id;

      Item item = this.Contents.Items.Contains(fileName);
      if (item==null)
      {
        // Step 5: Create the first version of the uploaded file
        item = new Item(fileName, storageDef, 1, this);
      }
      else
      {
        // Step 6: Update the version of a file
        item.Versions.Add(storageDef);
      }

      return item;
    }

    private Storage.StorageResponse CreateStorage(string fileName)
    {
      Storage.StorageRequest storageReq = new Storage.StorageRequest(this, fileName);
      Dictionary<string, string> headers = new Dictionary<string, string>();
      headers.AddHeader(PredefinedHeadersExtension.PredefinedHeaders.ContentTypeJson);
      headers.AddHeader(PredefinedHeadersExtension.PredefinedHeaders.AcceptJson);
      IRestResponse response = CallApi(string.Format("/data/v1/projects/{0}/storage", Owner.Json.id), Method.POST, headers, null, storageReq);
      return JsonConvert.DeserializeObject<JsonapiResponse<Storage.StorageResponse>>(response.Content).data;
    }



    #region "Folder Data Model Structure"

    public FolderResponse Json { get; set; }

    public class FolderResponse
    {
      public string type { get; set; }
      public string id { get; set; }
      public Relationships relationships { get; set; }
      public Attributes attributes { get; set; }
      public Links links { get; set; }

      public class Relationships
      {
        public Refs refs { get; set; }
        public Links links { get; set; }
        public Parent parent { get; set; }
        public Contents contents { get; set; }

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
        public class Contents
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
      }

      public class Attributes
      {
        public string displayName { get; set; }
        public string name { get; set; }
        public Extension extension { get; set; }
        public int objectCount { get; set; }
        public string createUserId { get; set; }
        public string lastModifiedUserId { get; set; }
        public string lastModifiedTime { get; set; }
        public string createTime { get; set; }

        public class Extension
        {
          public Data3 data { get; set; }
          public string version { get; set; }
          public string type { get; set; }
          public Schema schema { get; set; }

          public class Data3
          {
            public List<string> visibleTypes { get; set; }
            public List<string> allowedTypes { get; set; }
          }

          public class Schema
          {
            public string href { get; set; }
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
    }
    #endregion

  }
}
