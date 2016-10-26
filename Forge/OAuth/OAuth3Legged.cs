using Autodesk.Forge.Extensions;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autodesk.Forge.OAuth
{
  public class Token
  {
    public string token_type { get; set; }
    public int expires_in { get; set; }
    public string refresh_token { get; set; }
    public string access_token { get; set; }
  }

  public class OAuth3Legged : ForgeApi
  {
    public OAuth3Legged(string clientId, string clientSecret, string callbackUrl,
      string scopeInternal, string scopePublic)
    {
      ClientID = clientId;
      ClientSecret = clientSecret;
      CallbackUrl = callbackUrl;

      ScopeInternal = scopeInternal;
      ScopePublic = scopePublic;
    }

    private string ClientID { set; get; }
    private string ClientSecret { set; get; }
    private string CallbackUrl { get; set; }
    private string ScopeInternal { get; set; }
    private string ScopePublic { get; set; }

    public string AuthorizeUrl { get { return BASE_URL + string.Format("/authentication/v1/authorize?response_type=code&client_id={0}&redirect_uri={1}&scope={2}", ClientID, CallbackUrl, System.Net.WebUtility.UrlEncode(ScopeInternal)); } }

    public async Task<Token> GetToken(string code)
    {
      if (string.IsNullOrWhiteSpace(code)) return null;

      Dictionary<string, string> headers = new Dictionary<string, string>();
      headers.AddHeader(PredefinedHeadersExtension.PredefinedHeaders.ContentTypeFormUrlEncoded);

      Dictionary<string, string> parameters = new Dictionary<string, string>();
      parameters.Add("grant_type", "authorization_code");
      parameters.Add("client_id", ClientID);
      parameters.Add("client_secret", ClientSecret);
      parameters.Add("redirect_uri", CallbackUrl);
      parameters.Add("code", code);

      IRestResponse response = await MakeRequestAsync("/authentication/v1/gettoken", Method.POST, headers, parameters);
      return JsonConvert.DeserializeObject<Token>(response.Content);
    }

    public Token RefreshToken(string refreshToken)
    {
      throw new NotImplementedException();
    }
  }
}
