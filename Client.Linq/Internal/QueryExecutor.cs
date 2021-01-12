using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Remotion.Linq;

[assembly: InternalsVisibleTo("Client.Linq.Test, PublicKey=002400000480000094000000060200000024000052534131" +
                              "0004000001000100efaac865f88dd35c90dc548945405aae34056eedbe42cad60971f89a861a78437e86d" +
                              "95804a1aeeb0de18ac3728782f9dc8dbae2e806167a8bb64c0402278edcefd78c13dbe7f8d13de36eb362" +
                              "21ec215c66ee2dfe7943de97b869c5eea4d92f92d345ced67de5ac8fc3cd2f8dd7e3c0c53bdb0cc433af8" +
                              "59033d069cad397a7")]
namespace InfluxDB.Client.Linq.Internal
{
    /// <summary>
    /// Executor is called by ReLinq when query is executed.
    /// </summary>
    internal class InfluxDBQueryExecutor : IQueryExecutor
    {
        private readonly string _bucket;
        private readonly string _org;
        private readonly QueryApi _queryApi;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bucket">Specifies the source bucket.</param>
        /// <param name="org">Specifies the source organization.</param>
        /// <param name="queryApi">The underlying API to execute Flux Query.</param>
        public InfluxDBQueryExecutor(string bucket, string org, QueryApi queryApi)
        {
            _bucket = bucket;
            _org = org;
            _queryApi = queryApi;
        }

        /// <summary>
        /// Executes the given <paramref name="queryModel" /> as a scalar query,
        /// i.e. a query that ends with a aggregation operator such as Count, Sum, or Average.
        /// </summary>
        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            return ExecuteCollection<T>(queryModel).Single();
        }

        /// <summary>
        /// Executes the given <paramref name="queryModel" /> as a scalar query,
        /// i.e. a query that ends with a result operator such as First, Last, Single, Min, or Max.
        /// </summary>
        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            return returnDefaultWhenEmpty
                ? ExecuteCollection<T>(queryModel).SingleOrDefault()
                : ExecuteCollection<T>(queryModel).Single();
        }

        /// <summary>
        /// Executes a query with a collection result.
        /// </summary>
        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            var visitor = new InfluxDBQueryVisitor(_bucket);
            visitor.VisitQueryModel(queryModel);

            var task = _queryApi.QueryAsync<T>(visitor.GenerateQuery(), _org);
            return task.GetAwaiter().GetResult();
        }
    }
}