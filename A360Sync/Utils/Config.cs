namespace ForgeSampleA360Sync.Utils
{
  class Config
  {
    // In general it's not safe to store ID & secret on the user machine, this is
    // easy to reverse engineer and read the data. Ideally this piece would be done
    // online (server) where you can store your ID & secret, the user will authenticate
    // and get only a token to access his own data. 
    //
    // This sample intents to demonstrate local authentication and, therefore, uses 
    // this unsafe approach.
    // 
    public const string FORGE_CLIENT_ID = "";
    public const string FORGE_CLIENT_SECRET = "";
    public const string FORGE_CALLBACK_URL = "http://www.fake.com/api/forge/callback/oauth";

    public const string FORGE_SCOPE_INTERNAL = "data:read data:write data:create data:search bucket:create bucket:read bucket:update bucket:delete";
    public const string FORGE_SCOPE_PUBLIC = "data:read";
  }
}
