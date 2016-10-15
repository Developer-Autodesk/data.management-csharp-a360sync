using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autodesk.Forge.DataManagement
{
  public class Storage
  {
    #region Storage Response Data Model Structure

    public class StorageResponse
    {
      public string type { get; set; }
      public string id { get; set; }
      public Relationships relationships { get; set; }

      public class Relationships
      {
        public Target target { get; set; }

        public class Target
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
    }

    #endregion

    #region Storage Request Data Model Structure

    public class StorageRequest
    {
      public StorageRequest(Folder folder, string fileName)
      {
        jsonapi = new Jsonapi
        {
          version = "1.0",
        };

        data = new Data
        {
          type = "objects",
          attributes = new Data.Attributes
          {
            name = fileName
          },
          relationships = new Data.Relationships
          {
            target = new Data.Relationships.Target
            {
              data = new Data.Relationships.Target.Data
              {
                type = "folders",
                id = folder.Json.id
              }
            }
          }
        };
      }

      public Jsonapi jsonapi { get; set; }
      public Data data { get; set; }

      public class Jsonapi
      {
        public string version { get; set; }
      }

      public class Data
      {
        public string type { get; set; }
        public Attributes attributes { get; set; }
        public Relationships relationships { get; set; }

        public class Attributes
        {
          public string name { get; set; }
        }

        public class Relationships
        {
          public Target target { get; set; }

          public class Target
          {
            public Data data { get; set; }
            public class Data
            {
              public string type { get; set; }
              public string id { get; set; }
            }
          }
        }
      }
    }

    #endregion
  }
}
