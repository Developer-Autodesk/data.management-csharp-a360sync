﻿using Autodesk.Forge.OAuth;
using System;

namespace Autodesk.Forge.DataManagement
{
  public class A360 : ApiObject
  {
    public A360(Authorization accessToken) : base(accessToken)
    {
      Hubs = new DataManagement.HubsCollection(accessToken);
    }

    public HubsCollection Hubs { get; private set; }

    public string ID { get { throw new NotSupportedException(); }  }
  }
}
