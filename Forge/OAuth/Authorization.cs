using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autodesk.Forge.OAuth
{
  public class Authorization
  {
    public Authorization(string accessToken) { AccessToken = accessToken; }
    public string AccessToken { get; set; }
    public static Authorization Empty { get { return new Authorization(string.Empty); } }
  }
}
