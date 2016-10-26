# data.management-csharp-a360sync

![Platforms](https://img.shields.io/badge/platform-Windows-lightgray.svg)
![.NET](https://img.shields.io/badge/.NET-4.5.2-blue.svg)
[![ASP.NET](https://img.shields.io/badge/ASP.NET-4.5.2-blue.svg)](https://asp.net/)
[![License](http://img.shields.io/:license-mit-blue.svg)](http://opensource.org/licenses/MIT)

# Description

**IMPORTANT**: This still a work in progress, this sample is not yet fully tested. Use carefully, please. 

This sample aims to detect new files locally and upload them to the respective A360 account. It was written in C# for Windows (tested on Windows 10) and it includes 3 projects: 

**1. Forge**: Class Library (.DLL) that wraps some of OAuth, Data Management and OSS endpoints in a meanifull way.

**2. ServerOAuth**: ASP.NET project that handles OAuth authorization (3-legged). Forge Client ID & Secret are used here.

**3. A360Sync**: WinForm .EXE that mimic the user account structure locally and upload new files into his/her A360 Account.

## Demonstration

See [this video demonstration](https://www.youtube.com/watch?v=4Pgg05tLW-M).

# Setup

Install [Visual Studio 2015](https://www.visualstudio.com/).

Clone this project or download it. It's recommended to install [GitHub desktop](https://desktop.github.com/). To clone it via command line, use the following (**Git Shell** on Windows):

    git clone https://github.com/developer-autodesk/data.management-csharp-a360sync

For using this sample, you need an Autodesk developer credentials. Visit the [Forge Developer Portal](https://developer.autodesk.com), sign up for an account, then [create an app](https://developer.autodesk.com/myapps/create). For this new app, use **http://localhost:58966/autodeskcallback.aspx** as Callback URL. Finally take note of the **Client ID** and **Client Secret**.

At the **ServerOAuth** project, open the **web.config** file and adjust the appSettings:

  <appSettings>
    <add key="FORGE_CLIENT_ID" value="<<Your Client ID from Developer Portal>>" />
    <add key="FORGE_CLIENT_SECRET" value="<<Your Client Secret>>" />
    <add key="FORGE_CALLBACK_URL" value="http://localhost:58966/autodeskcallback.aspx"/>
  </appSettings>

The localhost port should be the same, but double check at Properties >> Web >> Servers >> Project URL setting.

Compile the solution, Visual Studio should download the NUGET packages ([RestSharp](https://www.nuget.org/packages/RestSharp) and [Newtonsoft.Json](https://www.nuget.org/packages/newtonsoft.json/))

Run should start the **ServerOAuth** web app and the **A360Sync** desktop app. The server has the ID & Secret and will redirect to the sign-in page, receive the callback and get the access_token. As the desktop app monitor the navigation, the server will redirect to a fake callback that the desktop app knows and allow it to store the token. 

# Deployment

You should not deploy this sample yet, it's not ready for production :-) (as of October 2016)

# Know issues

The desktop app uses a WebBrowser control, which is based on Internet Explorer. If a user mark the "Stay signed in" option, it will not be possible to log-off until "[Delete Browser History](https://support.microsoft.com/en-us/help/17438/windows-internet-explorer-view-delete-browsing-history)" on Internet Explorer. 

## Written by

Augusto Goncalves (Forge Partner Development)<br />
http://forge.autodesk.com<br />
