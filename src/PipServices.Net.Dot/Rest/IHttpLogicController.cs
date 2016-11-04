using System.Web.Http.Controllers;

namespace PipServices.Net.Rest
{
    public interface IHttpLogicController<T> : IHttpController
        where T : class 
    {
        T Logic { get; set; }
    }
}
