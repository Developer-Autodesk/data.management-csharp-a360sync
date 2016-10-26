using System.Web.Configuration;

namespace ServerOAuth
{
  internal class Config
  {
    internal static string FORGE_CLIENT_ID
    {
      get
      {
        return GetAppSetting("FORGE_CLIENT_ID");
      }
    }

    internal static string FORGE_CLIENT_SECRET
    {
      get
      {
        return GetAppSetting("FORGE_CLIENT_SECRET");
      }
    }

    internal static string FORGE_CALLBACK_URL
    {
      get
      {
        return GetAppSetting("FORGE_CALLBACK_URL");
      }
    }

    internal static string FORGE_SCOPE_INTERNAL
    {
      get
      {
        return "data:read data:write data:create data:search bucket:create bucket:read bucket:update bucket:delete";
      }
    }

    internal static string FORGE_SCOPE_PUBLIC
    {
      get
      {
        return "data:read";
      }
    }

    public static string GetAppSetting(string settingKey)
    {
      return WebConfigurationManager.AppSettings[settingKey];
    }

  }
}