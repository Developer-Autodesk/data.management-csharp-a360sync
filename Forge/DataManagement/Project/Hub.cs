using Autodesk.Forge.OAuth;
using Newtonsoft.Json;
using RestSharp;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Autodesk.Forge.DataManagement.Project
{
  public class HubsCollection : ApiObject, IEnumerable<Hub>
  {
    internal HubsCollection(Authorization accessToken) : base(accessToken)
    {

    }

    private IList<Hub> _hubs = null;

    IEnumerator IEnumerable.GetEnumerator()
    {
      return Enumerator().Result;
    }

    IEnumerator<Hub> IEnumerable<Hub>.GetEnumerator()
    {
      return Enumerator().Result;
    }

    private async Task<IEnumerator<Hub>> Enumerator()
    {
      if (_hubs == null)
      {
        _hubs = new List<Hub>();
        IRestResponse response = await CallApi("project/v1/hubs/", Method.GET);
        IList<Hub.HubResponse> hubsJsonData = JsonConvert.DeserializeObject<JsonapiResponse<IList<Hub.HubResponse>>>(response.Content).data;
        foreach (Hub.HubResponse hubJsonData in hubsJsonData)
        {
          Hub hub = new Hub(Authorization);
          hub.Json = hubJsonData;
          _hubs.Add(hub);
        }
          
      }
      return _hubs.GetEnumerator();
    }

    public Hub this[string hubId]
    {
      get
      {
        if (_hubs == null)
        {
          IRestResponse response = CallApi(string.Format("project/v1/hubs/{0}", hubId), Method.GET).Result;
          Hub.HubResponse hubJsonData = JsonConvert.DeserializeObject<JsonapiResponse<Hub.HubResponse>>(response.Content).data;
          Hub hub = new Hub(Authorization);
          hub.Json = hubJsonData;
          return hub;
        }
        foreach (Hub h in _hubs)
          if (h.Json.id.Equals(hubId))
            return h;
        return null; // should not happen...
      }
    }
  }

  public class Hub : ApiObject, IIdentifiable
  {
    private ProjectsCollection _projects = null;

    internal Hub() : base(Authorization.Empty)
    {
    }

    internal Hub(Authorization accessToken) : base(accessToken)
    {
      
    }

    public string ID
    {
      get
      {
        if (Json == null && string.IsNullOrEmpty(ID)) throw new System.Exception("Project ID is not valid");
        return Json.id;
      }
    }

    public ProjectsCollection Projects
    {
      get
      {
        if (_projects==null)
          _projects = new ProjectsCollection(this, Authorization);
        return _projects;
      }
    }

    //public override string ID { get { return Json.id; } }
    //public override string Name { get { return Json.attributes.name; } }

    #region "Data Model structure"

    public HubResponse Json { get; set; }

    public class HubResponse
    {
      public string type { get; set; }
      public string id { get; set; }
      public Attributes attributes { get; set; }
      public Links links { get; set; }
      public Relationships relationships { get; set; }

      public class Attributes
      {
        public class Extension
        {
          public class Schema
          {
            public string href { get; set; }
          }

          public class Data
          {
          }
          public string type { get; set; }
          public string version { get; set; }
          public Schema schema { get; set; }
          public Data data { get; set; }
        }
        public string name { get; set; }
        public Extension extension { get; set; }
      }

      public class Links
      {
        public class Self
        {
          public string href { get; set; }
        }
        public Self self { get; set; }
      }

      public class Related
      {
        public string href { get; set; }
      }

      public class Relationships
      {
        public class Projects
        {
          public class Links
          {
            public Related related { get; set; }
          }
          public Links links { get; set; }
        }
        public Projects projects { get; set; }
      }

    }
    #endregion
  }
}
