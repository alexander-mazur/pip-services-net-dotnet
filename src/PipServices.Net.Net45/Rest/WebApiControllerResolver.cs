using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Dispatcher;

namespace PipServices.Net.Rest
{
    internal class WebApiControllerResolver<T, I> : IDependencyResolver
        where T : class, IHttpLogicController<I>, new()
        where I : class
    {
        private readonly IDependencyResolver _baseResolver;
        private readonly I _logic;

        public WebApiControllerResolver(IDependencyResolver baseResolver, I logic)
        {
            if (logic == null)
                throw new ArgumentNullException(nameof(logic));

            _baseResolver = baseResolver;
            _logic = logic;
        }

        public IDependencyScope BeginScope()
        {
            return _baseResolver.BeginScope();
        }

        public void Dispose()
        {
            _baseResolver.Dispose();
        }

        public object GetService(Type serviceType)
        {
            // Substitude our controller selector
            if (serviceType == typeof(IHttpControllerSelector))
                return new ControllerSelector();

            // Substitude our controller activator
            if (serviceType == typeof(IHttpControllerActivator))
                return new ControllerActivator(_logic);

            return _baseResolver.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _baseResolver.GetServices(serviceType);
        }

        /// <summary>
        ///     Controller selector to select REST service for all requests
        /// </summary>
        private class ControllerSelector : IHttpControllerSelector
        {
            public IDictionary<string, HttpControllerDescriptor> GetControllerMapping()
            {
                var res = new Dictionary<string, HttpControllerDescriptor>();
                var desc = new HttpControllerDescriptor(
                    new HttpConfiguration(),
                    "", // All routes
                    typeof(T)
                    );
                res[""] = desc;
                return res;
            }

            public HttpControllerDescriptor SelectController(HttpRequestMessage request)
            {
                var desc = new HttpControllerDescriptor(
                    request.GetConfiguration(),
                    "", // Default controller name
                    typeof(T)
                    );
                return desc;
            }
        }

        /// <summary>
        ///     Controller activator to provide singleton reference to REST controller
        /// </summary>
        private class ControllerActivator : IHttpControllerActivator
        {
            public ControllerActivator(I logic)
            {
                Logic = logic;
            }

            private I Logic { get; }

            public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor,
                Type controllerType)
            {
                return new T { Logic = Logic };
            }
        }
    }
}
