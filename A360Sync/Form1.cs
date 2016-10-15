using System;
using System.Windows.Forms;
using System.Security.Permissions;
using System.Web;
using Autodesk.Forge.DataManagement;
using System.Text.RegularExpressions;
using System.IO;
using Autodesk.Forge;
using Autodesk.Forge.OAuth;
using System.Diagnostics;
using System.Collections.Generic;

namespace ForgeSampleA360Sync
{
  [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
  public partial class oAuthForm : Form
  {
    private const string FORGE_CLIENT_ID = "";
    private const string FORGE_CLIENT_SECRET = "";
    private const string FORGE_CALLBACK_URL = "";

    private WebBrowser2 wb = new WebBrowser2();
    private TextBox console = new TextBox();
    Autodesk.Forge.OAuth.OAuth3Legged _oAuth;
    Autodesk.Forge.OAuth.Token _token;
    Autodesk.Forge.DataManagement.A360 _a360;

    public oAuthForm()
    {
      Rest.OnUnauthorized += OAuth3Legged_OnForbidden;

      InitializeComponent();

      wb.Dock = DockStyle.Fill;
      wb.NavigateError += new WebBrowserNavigateErrorEventHandler(wb_NavigateError);
      Controls.Add(wb);

      _oAuth = new Autodesk.Forge.OAuth.OAuth3Legged(FORGE_CLIENT_ID, FORGE_CLIENT_SECRET, FORGE_CALLBACK_URL);
      wb.Navigate(_oAuth.AuthorizeUrl);

      
    }

    private void OAuth3Legged_OnForbidden(object sender, EventArgs e)
    {
      MessageBox.Show("Login again");
    }

    private void wb_NavigateError(
       object sender, WebBrowserNavigateErrorEventArgs e)
    {
      Uri callbackURL = new Uri(e.Url);
      if (e.Url.IndexOf(FORGE_CALLBACK_URL) == -1)
      {
        MessageBox.Show("Sorry, the authorization failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }
       
      // extract the code
      var q = HttpUtility.ParseQueryString(callbackURL.Query);
      _token = _oAuth.GetToken(q["code"]);

      /*
      System.Timers.Timer refreshTokenTimer = new System.Timers.Timer();
      refreshTokenTimer.Elapsed += RefreshTokenTimer_Elapsed;
      refreshTokenTimer.Interval = response.Data.expires_in * 0.9; // let's be safe and renew the token on 90% of the expiration time
      refreshTokenTimer.Enabled = true;
      */

      Controls.Remove(wb);
      SetUpSyncFolder();
    }

    /// <summary>
    /// Prepare a folder on MyDocuments/AUTODESK_USER_ID
    /// </summary>
    private void SetUpSyncFolder()
    {
      //_token = new Token();
      //_token.access_token = "f7pTguNQ7JgVVG4c7caSvCfxJAxz";
      
      // create a folder under MyDocuments with the username
      Me me = new Me(new Authorization(_token.access_token));
      string syncFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), FolderUtils.Sanitize(me.Json.userName));
      FolderUtils.EnsureFolderExists(syncFolder);

      // mimic the A360 folder structure (hubs, projects & folders)
      LogActivity("Checking folder structure...");
      _a360 = new A360(new Authorization(_token.access_token));
      foreach (Hub hub in _a360.Hubs)
      {
        string hubPath = Path.Combine(syncFolder, FolderUtils.Sanitize(hub.Json.attributes.name));
        FolderUtils.EnsureFolderExists(hubPath);
        FolderUtils.CreateIDFile(hubPath, "hub", hub.Json.id);

        foreach (Project project in hub.Projects)
        {
          string projectPath = Path.Combine(syncFolder, FolderUtils.Sanitize(hub.Json.attributes.name), FolderUtils.Sanitize(project.Json.attributes.name));
          FolderUtils.EnsureFolderExists(projectPath);
          FolderUtils.CreateIDFile(projectPath, "project", project.Json.id);
          var projectname = project.Json.attributes.name;

          foreach( Item item in project.RootFolder.Contents.Items)
          {
            //var name = item.Json.data.attributes.displayName;
          }
        }
      }
      LogActivity("Ready!");
      //Process.Start(syncFolder);
      

      // start monitoring the folders
      _monitor = new FileSystemWatcher();
      _monitor.Path = syncFolder;
      _monitor.NotifyFilter = NotifyFilters.LastAccess |  NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes | NotifyFilters.CreationTime;
      _monitor.IncludeSubdirectories = true;
      _monitor.Created += FileWasModified;
      _monitor.Changed += FileWasModified;
      _monitor.EnableRaisingEvents = true;

      // add a textbox as console output
      console.Dock = DockStyle.Fill;
      console.Multiline = true;
      console.ReadOnly = true;
      Controls.Add(console);
      LogActivity("Monitoring files...");
    }

    public const string IGNORE = ".tmp|.bak";

    private bool IsFileLocked(FileInfo file)
    {
      FileStream stream = null;

      try
      {
        stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
      }
      catch (IOException)
      {
        //the file is unavailable because it is:
        //still being written to
        //or being processed by another thread
        //or does not exist (has already been processed)
        return true;
      }
      finally
      {
        if (stream != null)
          stream.Close();
      }

      //file is not locked
      return false;
    }

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

      while (IsFileLocked(new FileInfo(originalFullPath)))
        System.Threading.Thread.Sleep(500);

      File.Copy(originalFullPath, tempFilePath);
     
      LogActivity(string.Format("File \"{0}\" detected", tempFilePath));

      string type;
      string id;
      if (!FolderUtils.ReadIDFile(Path.GetDirectoryName(originalFullPath), out type, out id))
      {
        LogActivity("Cannot process file");
        return;
      }

      switch (type)
      {
        case "hub":
          LogActivity("Cannot upload file to HUB, please move to Project or Folder");
          return;
        case "project":
          string projectId = id;
          LogActivity("Preparing to upload file...please wait.");
          FolderUtils.ReadIDFile(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(originalFullPath), @"..\")), out type, out id);
          UploadFile(tempFilePath, _a360.Hubs[id].Projects[projectId].RootFolder);
          LogActivity("File uploaded!");
          break;
      }
      File.Delete(tempFilePath); // this is the temp file
    }

    public void UploadFile(string filePath, Folder folder)
    {
      Item newItem = folder.UploadFile(filePath, true);
    }

    private void LogActivity(string activity)
    {
      if (console.InvokeRequired)
      {
        this.Invoke((MethodInvoker)delegate ()
        {
          LogActivity(activity);
        });
      }
      else
      {
        console.Text += string.Format("{0} - {1}{2}",
          DateTime.Now.ToLocalTime(),
          activity,
          Environment.NewLine);
        console.SelectionStart = console.Text.Length - 10;
        console.ScrollToCaret();
      }
    }

    private FileSystemWatcher _monitor = null;
  }
}
