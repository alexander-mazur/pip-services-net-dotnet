using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Dispatcher;
using PipServices.Commons.Refer;

namespace PipServices.Net.Rest
{
    internal class WebApiControllerResolver<T> : IDependencyResolver
        where T : class, IReferenceable, IHttpController, new()
    {
        private readonly IDependencyResolver _baseResolver;
        private readonly IReferences _references;

        public WebApiControllerResolver(IDependencyResolver baseResolver, IReferences references)
        {
            if (references == null)
                throw new ArgumentNullException(nameof(references));

            _baseResolver = baseResolver;
            _references = references;
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
                return new ControllerActivator(_references);

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
            public ControllerActivator(IReferences references)
            {
                References = references;
            }

            private IReferences References { get; }

            public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor,
                Type controllerType)
            {
                var controller = new T();
                controller.SetReferences(References);
                return controller;
            }
        }
    }
}
