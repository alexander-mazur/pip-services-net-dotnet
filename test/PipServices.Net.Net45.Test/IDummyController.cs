using PipServices.Commons.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PipServices.Net.Test
{
    public interface IDummyController
    {
        DataPage<Dummy> GetPageByFilter(string correlationId, FilterParams filter, PagingParams paging);
        Dummy GetOneById(string correlationId, string id);
        Dummy Create(string correlationId, Dummy entity);
        Dummy Update(string correlationId, Dummy entity);
        Dummy DeleteById(string correlationId, string id);
    }
}
