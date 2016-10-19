using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Formatters;
using PipServices.Net.Rest;
using PipServices.Commons.Refer;
using PipServices.Commons.Data;
using PipServices.Net.Test;

namespace PipServices.Net.Test.Rest
{
    public sealed class DummyRestClient : RestClient, IDummyService, IDescriptable
    {
        public static Descriptor Descriptor { get; } = new Descriptor("pip-services-dummies", "client", "rest", "1.0");

        public DummyRestClient()
            : base("dummies")
        {
        }

        public Descriptor GetDescriptor()
        {
            return Descriptor;
        }

        public DataPage<Dummy> GetPageByFilter(string correlationId, FilterParams filter, PagingParams paging)
        {

            var timing = Instrument(correlationId, "dummy.get_page_by_filter");
            try
            {
                return _resource
                    .queryParams(new RestQueryParams(correlationId, filter, paging))
                    .type(MediaType.APPLICATION_JSON)
                    .get(new GenericType<DataPage<Dummy>>() {});
            }
            finally
            {
                timing.EndTiming();
            }
        }

        public Dummy GetOneById(string correlationId, string id)
        {
            var timing = Instrument(correlationId, "dummy.get_one_by_id");
            try
            {
                return _resource.path(id)
                    .queryParams(new RestQueryParams(correlationId))
                    .type(MediaType.APPLICATION_JSON)
                    .get(Dummy);
            }
            finally
            {
                timing.EndTiming();
            }
        }

        public Dummy Create(string correlationId, Dummy entity)
        {
            var timing = Instrument(correlationId, "dummy.create");
            try
            {
                return _resource
                    .queryParams(new RestQueryParams(correlationId))
                    .type(MediaType.APPLICATION_JSON)
                    .post(Dummy, entity);
            }
            finally
            {
                timing.EndTiming();
            }
        }

        public Dummy Update(string correlationId, Dummy entity)
        {
            var timing = Instrument(correlationId, "dummy.update");
            try
            {
                return _resource.path(entity.getId())
                    .queryParams(new RestQueryParams(correlationId))
                    .type(MediaType.APPLICATION_JSON)
                    .put(Dummy, entity);
            }
            finally
            {
                timing.EndTiming();
            }
        }

        public Dummy DeleteById(string correlationId, string id)
        {
            var timing = Instrument(correlationId, "dummy.delete_by_id");
            try
            {
                return _resource.path(id)
                    .queryParams(new RestQueryParams(correlationId))
                    .type(MediaType.APPLICATION_JSON)
                    .delete(Dummy)
                ;
            }
            finally
            {
                timing.EndTiming();
            }
        }
    }
}
