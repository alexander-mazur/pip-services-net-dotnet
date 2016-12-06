using System.Collections.Generic;
using PipServices.Commons.Data;
using PipServices.Commons.Refer;

namespace PipServices.Net
{
    public sealed class DummyController : IDummyController, IDescriptable
    {
        public static Descriptor Descriptor { get; } = new Descriptor("pip-services-dummies", "controller", "default", "default", "1.0");

        private readonly object _lock = new object();
        private readonly IList<Dummy> _entities = new List<Dummy>();

        public Descriptor GetDescriptor()
        {
            return Descriptor;
        }

        public DataPage<Dummy> GetPageByFilter(string correlationId, FilterParams filter, PagingParams paging)
        {
            filter = filter != null ? filter : new FilterParams();
            var key = filter.GetAsNullableString("key");

            paging = paging != null ? paging : new PagingParams();
            var skip = paging.GetSkip(0);
            var take = paging.GetTake(100);

            var result = new List<Dummy>();

            lock(_lock)
            {
                foreach (var entity in _entities)
                {
                    if (key != null && !key.Equals(entity.Key))
                        continue;

                    skip--;
                    if (skip >= 0) continue;

                    take--;
                    if (take < 0) break;

                    result.Add(entity);
                }
            }
            return new DataPage<Dummy>(result);
        }

        public Dummy GetOneById(string correlationId, string id)
        {
            lock(_lock)
            {
                foreach(var entity in _entities)
                {
                    if (entity.Id.Equals(id))
                        return entity;
                }
            }
            return null;
        }

        public Dummy Create(string correlationId, Dummy entity)
        {
            lock(_lock)
            {
                if (entity.Id == null)
                    entity.Id = IdGenerator.NextLong();

                _entities.Add(entity);
            }
            return entity;
        }

        public Dummy Update(string correlationId, Dummy newEntity)
        {
            lock(_lock)
            {
                for (int index = 0; index < _entities.Count; index++)
                {
                    var entity = _entities[index];
                    if (entity.Id.Equals(newEntity.Id))
                    {
                        _entities[index] = newEntity;
                        return newEntity;
                    }
                }
            }
            return null;
        }

        public Dummy DeleteById(string correlationId, string id)
        {
            lock(_lock)
            {
                for (int index = 0; index < _entities.Count; index++)
                {
                    var entity = _entities[index];
                    if (entity.Id.Equals(id))
                    {
                        _entities.RemoveAt(index);
                        return entity;
                    }
                }
            }
            return null;
        }
    }
}
