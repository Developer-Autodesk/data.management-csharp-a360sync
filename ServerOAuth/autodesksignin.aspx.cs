using Autodesk.Forge.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ServerOAuth
{
  public partial class autodesksignin : System.Web.UI.Page
  {
    protected void Page_Load(object sender, EventArgs e)
    {
      Autodesk.Forge.OAuth.OAuth3Legged oauth = new OAuth3Legged(Config.FORGE_CLIENT_ID,
        Config.FORGE_CLIENT_SECRET, Config.FORGE_CALLBACK_URL,
        Config.FORGE_SCOPE_INTERNAL, Config.FORGE_SCOPE_PUBLIC);

      Response.Redirect(oauth.AuthorizeUrl);
    }
  }
}