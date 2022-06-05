

namespace MoreNote.Logic.Entity.ConfigFile
{
    public class WebSiteConfig
    {
        public bool IsAlreadyInstalled { get; set; }
    
       
        public PostgreSqlConfig PostgreSql { get; set; }
        public RandomImangeServiceConfig PublicAPI { get; set; }

        public APPConfig APPConfig { get; set; } = new APPConfig();


        public WebSiteConfig()
        {


        }
        public static WebSiteConfig GenerateTemplate()
        {
            WebSiteConfig webSiteConfig=new WebSiteConfig()
            {
                IsAlreadyInstalled=false,
            };
            return webSiteConfig;
        }

    }
}