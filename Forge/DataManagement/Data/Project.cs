using Autodesk.Forge.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autodesk.Forge.DataManagement.Data
{
  public class Project : ApiObject
  {
    internal DataManagement.Project.Project Owner { get; set; }

    internal Project(DataManagement.Project.Project project , Authorization auth) : base(auth)
    {
      Owner = project;

      /*this.Json = new ProjectResponse
      {
        id = projectId
      };*/
    }

    private string _id;

    /// <summary>
    /// Shallow version of Data.Project
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="auth"></param>
    internal Project(string projectId, Authorization auth) : base (auth)
    {
      _id = projectId;
    }

    public string ID
    {
      get
      {
        return (Owner != null ? Owner.ID : _id);
      }
    }

    /// <summary>
    /// Return a Folder object skeleton, regardless how deep it is on the current project structure.
    /// </summary>
    /// <param name="folderId"></param>
    /// <returns></returns>
    public Folder FolderById(Folder.FolderID folderId)
    {
      return new Folder(this.ID, folderId, this.Authorization);

      // This recursive call can trigger download all structure
      /*
      Folder ret =null;
      RecursiveFolderById(ret, this.RootFolder, folderId);
      return ret;
      */
    }

    /*
    private void RecursiveFolderById(Folder ret, Folder folder, string folderId)
    {
      // check this level..
      foreach (Folder f in folder.Contents.Folders) {
        if (f.Json.id.Equals(folderId)) {
          ret = f;
          return;
        }
      }

      // go to next level
      foreach (Folder f in folder.Contents.Folders)
      {
        RecursiveFolderById(ret, f, folderId);
        if (ret != null) return;
      }
    }
    */

    //public override string ID { get { return Json.id; } }
    //public override string Name { get { return Json.attributes.name; } }

    private Folder _rootFolder = null;

    public Folder RootFolder
    {
      get
      {
        if (_rootFolder == null)
          _rootFolder = new Folder(this, new Folder.FolderID(Owner.Json.relationships.rootFolder.data.id));
        return _rootFolder;
      }
    }
  }
}
