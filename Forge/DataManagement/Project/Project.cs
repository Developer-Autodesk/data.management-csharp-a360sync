using Autodesk.Forge.OAuth;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Autodesk.Forge.DataManagement.Project
{
  public class ProjectsCollection : ApiObject, IEnumerable<Project>
  {
    private IList<Project> _projects = null;
    private Hub Owner { get; set; }

    internal ProjectsCollection() : base(Authorization.Empty) { }

    internal ProjectsCollection(Hub owner, Authorization auth) : base(auth)
    {
      Owner = owner;
    }

    public IEnumerator<Project> GetEnumerator()
    {
      return Enumerator().Result;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return Enumerator().Result;
    }

    private async Task<IEnumerator<Project>> Enumerator()
    {
      if (_projects == null)
      {
        _projects = new List<Project>();
        IRestResponse response = await CallApi(string.Format("project/v1/hubs/{0}/projects", Owner.Json.id), Method.GET);
        IList<Project.ProjectResponse> projectsJsonData = JsonConvert.DeserializeObject<JsonapiResponse<IList<Project.ProjectResponse>>>(response.Content).data;
        foreach (Project.ProjectResponse projectJsonData in projectsJsonData)
        {
          Project project = new Project(Authorization);
          project.Json = projectJsonData;
          _projects.Add(project);
        }

        foreach (Project project in _projects)
          project.Owner = Owner;
      }
      return _projects.GetEnumerator();
    }

    public Project this[string projectId]
    {
      get
      {
        if (_projects == null)
        {
          return new Project(this.Owner, projectId, Authorization);
        }
        foreach (Project h in _projects)
          if (h.Json.id.Equals(projectId))
            return h;
        return null; // should not happen...
      }
    }
  }

  public class Project : ApiObject, IIdentifiable
  {
    internal Hub Owner { get; set; }

    internal Project() : base(Authorization.Empty) { }
    internal Project(Authorization auth) : base(auth) { }

    internal Project(Hub owner, string projectId, Authorization auth) : base(auth)
    {
      Owner = owner;
      IRestResponse response = CallApi(string.Format("project/v1/hubs/{0}/projects/{1}", Owner.Json.id, projectId), Method.GET).Result;
      Project.ProjectResponse projectJsonData = JsonConvert.DeserializeObject<JsonapiResponse<Project.ProjectResponse>>(response.Content).data;
      this.Json = projectJsonData;
    }

    public string ID
    {
      get
      {
        if (Json == null && string.IsNullOrEmpty(ID)) throw new System.Exception("Project ID is not valid");
        return Json.id;
      }
    }

    public Data.Project DataProject
    {
      get
      {
        Data.Project p = new Data.Project(this, Authorization);
        p.Owner = this;
        return p;
      }
    }

    #region "Data Model Structure"

    public ProjectResponse Json { get; set; }

    public class ProjectResponse
    {

      public string type { get; set; }
      public string id { get; set; }
      public Attributes attributes { get; set; }
      public Relationships relationships { get; set; }
      public Links links { get; set; }

      public class Attributes
      {
        public string name { get; set; }
        public Extension extension { get; set; }

        public class Extension
        {
          public Data data { get; set; }
          public string version { get; set; }
          public string type { get; set; }
          public Schema schema { get; set; }

          public class Data
          {
          }

          public class Schema
          {
            public string href { get; set; }
          }
        }
      }

      public class Relationships
      {
        public RootFolder rootFolder { get; set; }
        public Hub hub { get; set; }

        public class RootFolder
        {
          public Meta meta { get; set; }
          public Data data { get; set; }

          public class Meta
          {
            public Link link { get; set; }

            public class Link
            {
              public string href { get; set; }
            }
          }

          public class Data
          {
            public string type { get; set; }
            public string id { get; set; }
          }
        }

        public class Hub
        {
          public Data data { get; set; }
          public Links links { get; set; }

          public class Data
          {
            public string type { get; set; }
            public string id { get; set; }
          }

          public class Links
          {
            public Related related { get; set; }

            public class Related
            {
              public string href { get; set; }
            }
          }
        }
      }

      public class Links
      {
        public Self self { get; set; }

        public class Self
        {
          public string href { get; set; }
        }
      }
    }

    #endregion
  }
}
