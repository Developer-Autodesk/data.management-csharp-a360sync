using Autodesk.Forge.DataManagement.Data;
using Autodesk.Forge.DataManagement.Project;
using Autodesk.Forge.OAuth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace ForgeSampleA360Sync
{
  class FolderMonitor
  {
    Autodesk.Forge.OAuth.Token _token;
    Autodesk.Forge.DataManagement.Project.A360 _a360;

    public FolderMonitor(Token token)
    {
      _token = token;
    }

    public void StartMonitorting(string path)
    {

      new System.Threading.Thread(() =>
      {
        System.Threading.Thread.CurrentThread.IsBackground = true;
        SetUpSyncFolder(path);
      }).Start();
    } 

    /// <summary>
    /// Prepare a folder on MyDocuments/AUTODESK_USER_ID
    /// </summary>
    private void SetUpSyncFolder(string path)
    {
      LogActivity(string.Format("Sync folder: {0}", path));

      

      // create a folder under MyDocuments with the username
      Me me = new Me(new Authorization(_token.access_token));
      string syncFolder = Path.Combine(path, Utils.FolderUtils.Sanitize(me.Json.userName));
      Utils.FolderUtils.EnsureFolderExists(syncFolder);

      

      // mimic the A360 folder structure (hubs, projects & folders)
      LogActivity("Checking folder structure under");
      _a360 = new A360(new Authorization(_token.access_token));

      
      foreach (Hub hub in _a360.Hubs)
      {
        string hubPath = Path.Combine(syncFolder, Utils.FolderUtils.Sanitize(hub.Json.attributes.name));
        Utils.FolderUtils.EnsureFolderExists(hubPath);
        Utils.FolderUtils.CreateIDFile(hubPath, "hub", hub.Json.id);
        LogActivity(string.Format("Hub: {0}", hub.Json.attributes.name));

        foreach (Autodesk.Forge.DataManagement.Project.Project project in hub.Projects)
        {
          string projectPath = Path.Combine(syncFolder, Utils.FolderUtils.Sanitize(hub.Json.attributes.name), Utils.FolderUtils.Sanitize(project.Json.attributes.name));
          Utils.FolderUtils.EnsureFolderExists(projectPath);
          Utils.FolderUtils.CreateIDFile(projectPath, "project", project.Json.id);
          LogActivity(string.Format("Project: {0}", project.Json.attributes.name));
          //var projectname = project.Json.attributes.name;

          FolderContents(projectPath, project.DataProject.RootFolder);
        }
      }
      LogActivity("Ready!");
      //Process.Start(syncFolder);

  
      // start monitoring the folders
      _monitor = new FileSystemWatcher();
      _monitor.Path = syncFolder;
      _monitor.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes | NotifyFilters.CreationTime;
      _monitor.IncludeSubdirectories = true;
      _monitor.Created += FileWasModified;
      _monitor.Changed += FileWasModified;
      _monitor.EnableRaisingEvents = true;

      LogActivity("Monitoring files...");
    }

    public void FolderContents(string projectPath, Folder upperFolder)
    {
      foreach (Folder folder in upperFolder.Contents.Folders)
      {
        string folderPath = Path.Combine(projectPath, Utils.FolderUtils.Sanitize(folder.Json.attributes.name));
        Utils.FolderUtils.EnsureFolderExists(folderPath);
        Utils.FolderUtils.CreateIDFile(folderPath, "folder", folder.Json.id);

        LogActivity(string.Format("    Subfolder {0}", folder.Json.attributes.displayName));
        FolderContents(folderPath, folder);
      }

      foreach (Item item in upperFolder.Contents.Items)
      {
        LogActivity(string.Format("    Items {0}", item.Json.attributes.displayName));
      }
    }

    public const string IGNORE = ".tmp|.bak";

    private Dictionary<string, DateTime> _lastEventRaised = new Dictionary<string, DateTime>();

    private void FileWasModified(object sender, FileSystemEventArgs e)
    {
      string originalFullPath = e.FullPath;
      string tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(originalFullPath));

      FileInfo f = new FileInfo(originalFullPath);
      if (f.Attributes.HasFlag(FileAttributes.Hidden) ||
        f.Attributes.HasFlag(FileAttributes.Directory) ||
        string.IsNullOrWhiteSpace(Path.GetExtension(originalFullPath)) ||
        IGNORE.Contains(Path.GetExtension(originalFullPath)))
      {
        return;
      }

      if (_lastEventRaised.ContainsKey(originalFullPath))
      {
        if (f.LastWriteTime < _lastEventRaised[originalFullPath])
          return;

        if ((DateTime.Now - _lastEventRaised[originalFullPath]).TotalMilliseconds < 1000)
        {
          _lastEventRaised[originalFullPath] = DateTime.Now;
          return;
        }
      }
      else
        _lastEventRaised.Add(originalFullPath, DateTime.Now);

      if (File.Exists(tempFilePath)) File.Delete(tempFilePath);

      // need to wait until the file is not locked..
      while (Utils.FolderUtils.IsFileLocked(new FileInfo(originalFullPath)))
        System.Threading.Thread.Sleep(500);

      // make a copy as we'll read it.
      File.Copy(originalFullPath, tempFilePath);

      LogActivity(string.Format("File \"{0}\" detected", originalFullPath));

      string type;
      string id;
      if (!Utils.FolderUtils.ReadIDFile(Path.GetDirectoryName(originalFullPath), out type, out id))
      {
        LogActivity("Cannot process file");
        return;
      }

      string projectId = string.Empty ;
      string hubId = string.Empty;
      Item newItem;
      switch (type)
      {
        case "hub":
          LogActivity("Cannot upload file to HUB, please move to Project or Folder");
          return;
        case "project":
         projectId = id;
          LogActivity("Preparing to upload file...please wait.");
          Utils.FolderUtils.ReadIDFile(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(originalFullPath), @"..\")), out type, out id);
          hubId = id;
         newItem =  UploadFile(tempFilePath, _a360.Hubs[hubId].Projects[projectId].DataProject.RootFolder).Result;
          LogActivity("File uploaded!");
          break;
        case "folder":
          string folderId = id;

          string upperFolder = originalFullPath;
          do
          {
            upperFolder = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(upperFolder), @"..\"));
            Utils.FolderUtils.ReadIDFile(upperFolder, out type, out id);
          } while (type != "project");

          projectId = id;

          Utils.FolderUtils.ReadIDFile(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(upperFolder), @"..\")), out type, out id);
          hubId = id;

          Folder folder = _a360.Hubs[hubId].Projects[projectId].DataProject.FolderById(new Folder.FolderID( folderId));
          newItem = UploadFile(tempFilePath, folder).Result;
          LogActivity("File uploaded!");
          break;
      }
      File.Delete(tempFilePath); // this is the temp file
    }

    private async Task<Item> UploadFile(string filePath, Folder folder)
    {
      Item newItem = await folder.UploadFile(filePath, true);
      return newItem;
    }

    public event EventHandler<ActivityEventArgs> OnActivity;

    private void LogActivity(string message)
    {
      OnActivity?.Invoke(this, new ActivityEventArgs(message));
    }


    public class ActivityEventArgs : EventArgs
    {
      public ActivityEventArgs(string message) { Message = message; }
      public string Message { get; set; }
    }

    private FileSystemWatcher _monitor = null;
  }
}
