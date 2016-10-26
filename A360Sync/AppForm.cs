using System;
using System.Windows.Forms;
using System.Security.Permissions;
using System.Web;
using Autodesk.Forge.OAuth;
using System.Threading.Tasks;

namespace ForgeSampleA360Sync
{
  [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
  public partial class oAuthForm : Form
  {
    private WebBrowser2 wb = new WebBrowser2();
    private TextBox console = new TextBox();

    Autodesk.Forge.OAuth.OAuth3Legged _oAuth = null;
    Autodesk.Forge.OAuth.Token _token = null;

    FolderMonitor _monitor;

    public oAuthForm()
    {
      Utils.RegistryUtils.SetBrowserFeatureControl();

      InitializeComponent();

      wb.Dock = DockStyle.Fill;
      wb.NavigateError += new WebBrowserNavigateErrorEventHandler(wb_NavigateError);
      wb.Navigated += Wb_Navigated;
      Controls.Add(wb);

      wb.Navigate(Utils.Config.AUTHORIZE_URL);
    }

    private void Wb_Navigated(object sender, WebBrowserNavigatedEventArgs e)
    {
      CheckURL(e.Url);
    }

    public async Task CheckURL(Uri url)
    {
      if (url.Host.IndexOf("auth.autodesk.com") > -1)
      {
        // the scopes pages is a bit bigger... let's resize
        this.Height = (int)(this.Height * 1.7 > Screen.PrimaryScreen.WorkingArea.Height ? Screen.PrimaryScreen.WorkingArea.Height : this.Height * 1.5);
        this.CenterToScreen();
        return;
      }

      if (url.AbsoluteUri.IndexOf(Utils.Config.FAKE_CALLBACK_URL) == 0)
      {
        if (_token != null) return;

        var q = HttpUtility.ParseQueryString(url.Query);
        if (string.IsNullOrWhiteSpace(q["access_token"])) return;

        _token = new Token();
        _token.access_token = q["access_token"];

        /*
        System.Timers.Timer refreshTokenTimer = new System.Timers.Timer();
        refreshTokenTimer.Elapsed += RefreshTokenTimer_Elapsed;
        refreshTokenTimer.Interval = response.Data.expires_in * 0.9; // let's be safe and renew the token on 90% of the expiration time
        refreshTokenTimer.Enabled = true;
        */

        PrepareMonitor();
      }
    }

    private void wb_NavigateError(
       object sender, WebBrowserNavigateErrorEventArgs e)
    {
      CheckURL(new Uri(e.Url));
    }

    private void PrepareMonitor()
    {
      Controls.Remove(wb);

      // add a textbox as console output
      console.Dock = DockStyle.Fill;
      console.Multiline = true;
      console.ReadOnly = true;
      Controls.Add(console);

      _monitor = new ForgeSampleA360Sync.FolderMonitor(_token);
      _monitor.OnActivity += _monitor_OnActivity;
      _monitor.StartMonitorting(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
    }

    private void _monitor_OnActivity(object sender, FolderMonitor.ActivityEventArgs e)
    {
      LogActivity(e.Message);
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

  }
}
