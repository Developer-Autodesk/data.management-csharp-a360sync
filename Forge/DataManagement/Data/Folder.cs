using Autodesk.Forge.Extensions;
using Autodesk.Forge.OAuth;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Autodesk.Forge.DataManagement.Data
{
  public class FolderCollection : ApiObject, IEnumerable<Folder>
  {
    private Folder Owner { get; set; }

    internal FolderCollection(Folder owner, Authorization accessToken) : base(accessToken)
    {
      Owner = owner;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return Enumerator().Result;
    }

    IEnumerator<Folder> IEnumerable<Folder>.GetEnumerator()
    {
      return Enumerator().Result;
    }

    /// <summary>
    /// Return a item with same displayName
    /// </summary>
    /// <param name="displayName">The display name (e.g. MyFile.dwg)</param>
    /// <returns></returns>
    public Folder Contains(string displayName)
    {
      foreach (Folder f in this._folders)
        if (f.Json.attributes.displayName.Equals(displayName))
          return f;
      return null;
    }

    private IList<Folder> _folders = null;

    internal void Invalidate()
    {
      _folders.Clear();
      _folders = null;
    }

    private async Task<IEnumerator<Folder>> Enumerator()
    {
      if (_folders == null)
      {
        _folders = new List<Folder>();
        IRestResponse response = await CallApi(string.Format("data/v1/projects/{0}/folders/{1}/contents", Owner.Owner.ID, System.Uri.EscapeUriString(Owner.ID)), Method.GET);
        IList<Folder.FolderResponse> foldersJsonData = JsonConvert.DeserializeObject<JsonapiResponse<IList<Folder.FolderResponse>>>(response.Content).data;
        foreach (Folder.FolderResponse folderJsonData in foldersJsonData)
        {
          if (!folderJsonData.type.Equals("folders")) continue;
          Folder folder = new Folder(this.Owner.Owner, new Folder.FolderID( folderJsonData.id));
          folder.Json = folderJsonData;
          _folders.Add(folder);
        }
      }
      return _folders.GetEnumerator();
    }
  }

  public class Folder : ApiObject, IIdentifiable
  {
    public struct FolderID
    {
      public FolderID(string id) { ID = id; }
      public string ID { get; set; }
    }

    private Project _owner = null;
    internal Project Owner
    {
      get
      {
        if (_owner == null)
        {
          Uri u = new Uri(this.Json.links.self.href);
          string projectId = u.Segments[4].TrimEnd('/');
          _owner = new Data.Project(projectId, Authorization);
        }
        return _owner;
      }
      set { _owner = value; }
    }

    internal Folder(Project owner, FolderID folderId) : base(owner.Authorization)
    {
      Owner = owner;
      Init(owner.ID, folderId);
    }

    internal Folder(string projectId, FolderID folderId, Authorization auth) : base(auth)
    {
      Init(projectId, folderId);
    }

    public string ID
    {
      get
      {
        if (Json == null || string.IsNullOrEmpty(Json.id)) throw new System.Exception("Folder ID is not valid");
        return Json.id;
      }
    }

    private void Init(string projectId, FolderID folderId)
    {
      IRestResponse response = CallApi(string.Format("data/v1/projects/{0}/folders/{1}", projectId, folderId.ID ), Method.GET).Result;
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

      private FolderCollection _folders;
      public FolderCollection Folders {
        get
        {
          if (_folders == null)
            _folders = new FolderCollection(Owner, this.Authorization);
          return _folders;
        }
      }
    }

    //public override string ID { get { return Json.id; } }
    //public override string Name { get { return Json.attributes.name; } }

    /// <summary>
    /// Upload a files to the folder
    /// </summary>
    /// <param name="filePath">Full path of the file</param>
    /// <param name="createNewVersion">If file already on the folder, TRUE will create new version, FALSE will do nothing</param>
    /// <returns>The newly created item</returns>
    public async Task<Item> UploadFile(string filePath, bool createNewVersion)
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
      Storage.StorageResponse storageDef = await CreateStorage(fileName);

      // Step 4: Upload a file to the storage location
      OSS.Bucket bucket = new OSS.Bucket(Authorization);
      string bucketKey, objectName;
      OSS.Bucket.Extract(storageDef.id, out bucketKey, out objectName);
      await bucket.UploadFile(filePath, bucketKey, objectName);

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
        await item.Versions.Add(storageDef);
      }

      return item;
    }

    private async Task<Storage.StorageResponse> CreateStorage(string fileName)
    {
      Storage.StorageRequest storageReq = new Storage.StorageRequest(this, fileName);
      Dictionary<string, string> headers = new Dictionary<string, string>();
      headers.AddHeader(PredefinedHeadersExtension.PredefinedHeaders.ContentTypeJson);
      headers.AddHeader(PredefinedHeadersExtension.PredefinedHeaders.AcceptJson);
      IRestResponse response = await CallApi(string.Format("/data/v1/projects/{0}/storage", Owner.ID), Method.POST, headers, null, storageReq);
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
