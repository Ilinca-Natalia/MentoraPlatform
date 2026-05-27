using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MentoraPlatform.Startup))]
namespace MentoraPlatform
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
