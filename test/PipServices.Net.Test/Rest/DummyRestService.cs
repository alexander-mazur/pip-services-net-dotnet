﻿using System;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Server;
using PipServices.Net.Rest;
using PipServices.Commons.Refer;

namespace PipServices.Net.Test.Rest
{
    public sealed class DummyRestService : RestService<Startup>, IDescriptable
    {
        public static Descriptor Descriptor { get; } = new Descriptor("pip-services-dummies", "service", "rest", "1.0");

        private IDummyService _controller;

        public DummyRestService()
        {
        }

        public Descriptor GetDescriptor()
        {
            return Descriptor;
        }

        public override void SetReferences(IReferences references)
        {
            _controller =
                (IDummyService)
                    references.GetOneBefore(this, new Descriptor("pip-services-dummies", "controller", "*", "*"));

            base.SetReferences(references);
        }
    }
}