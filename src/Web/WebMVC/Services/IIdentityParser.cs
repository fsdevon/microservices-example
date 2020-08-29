using System;
using System.Security.Principal;

namespace MicroservicesExample.Web.WebMVC.Services
{
    public interface IIdentityParser<T>
    {
        T Parse(IPrincipal principal);
    }
}
