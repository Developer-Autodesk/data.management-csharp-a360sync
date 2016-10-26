using Newtonsoft.Json;
using RestSharp;
using System.Threading.Tasks;

namespace Autodesk.Forge.OAuth
{
  public class Me : ApiObject
  {
    public Me() : base(Authorization.Empty) { }

    public Me(Authorization accessToken) : base(accessToken)
    {
      IRestResponse response = CallApi("/userprofile/v1/users/@me", Method.GET).Result;

      if (response == null) return;
      UserMeResponse meJsonData = JsonConvert.DeserializeObject<UserMeResponse>(response.Content);
      this.Json = meJsonData;
    }

    #region "Data Model Structure"

    public UserMeResponse Json { get; set; }

    public class UserMeResponse
    {
      public string userId { get; set; }
      public string userName { get; set; }
      public string emailId { get; set; }
      public string firstName { get; set; }
      public string lastName { get; set; }
    }

    #endregion
  }
}
