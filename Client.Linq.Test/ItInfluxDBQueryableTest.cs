using System;
using System.Linq;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using InfluxDB.Client.Core.Test;
using InfluxDB.Client.Linq;
using NUnit.Framework;

namespace Client.Linq.Test
{
    [TestFixture]
    public class ItInfluxDBQueryableTest : AbstractTest
    {
        private InfluxDBClient _client;

        [SetUp]
        public new async Task SetUp()
        {
            _client = InfluxDBClientFactory.Create(GetInfluxDb2Url(), "my-token");
            _client.SetLogLevel(LogLevel.Body);

            // DateTime(2020, 10, 15, 8, 20, 15, DateTimeKind.Utc)
            const string sensor11 = "sensor,deployment=production,sensor_id=id-1 data=15 1602750015";
            const string sensor21 = "sensor,deployment=production,sensor_id=id-2 data=15 1602750015";
            // new DateTime(2020, 11, 15, 8, 20, 15, DateTimeKind.Utc)
            const string sensor12 = "sensor,deployment=production,sensor_id=id-1 data=28 1605428415";
            const string sensor22 = "sensor,deployment=production,sensor_id=id-2 data=28 1605428415";
            // new DateTime(2020, 11, 16, 8, 20, 15, DateTimeKind.Utc)
            const string sensor13 = "sensor,deployment=production,sensor_id=id-1 data=12 1605514815";
            const string sensor23 = "sensor,deployment=production,sensor_id=id-2 data=12 1605514815";
            // new DateTime(2020, 11, 17, 8, 20, 15, DateTimeKind.Utc)
            const string sensor14 = "sensor,deployment=production,sensor_id=id-1 data=89 1605601215";
            const string sensor24 = "sensor,deployment=production,sensor_id=id-2 data=89 1605601215";

            await _client
                .GetWriteApiAsync()
                .WriteRecordsAsync("my-bucket", "my-org", WritePrecision.S, 
                    sensor11, sensor21, sensor12, sensor22, sensor13, sensor23, sensor14, sensor24);
        }

        [Test]
        public void QueryAll()
        {
            var query = from s in InfluxDBQueryable<Sensor>.Queryable("my-bucket", "my-org", _client.GetQueryApi())
                select s;

            var sensors = query.ToList();

            Assert.AreEqual(8, sensors.Count);
        }

        [Test]
        public void QueryTake()
        {
            var query = (from s in InfluxDBQueryable<Sensor>.Queryable("my-bucket", "my-org", _client.GetQueryApi())
                select s).Take(2);

            var sensors = query.ToList();

            Assert.AreEqual(2*2, sensors.Count);
        }

        [Test]
        public void QueryTakeSkip()
        {
            var query = (from s in InfluxDBQueryable<Sensor>.Queryable("my-bucket", "my-org", _client.GetQueryApi())
                select s).Take(2).Skip(3);

            var sensors = query.ToList();

            Assert.AreEqual(1+1, sensors.Count);
        }
        
        [Test]
        public void QueryWhere()
        {
            var query = from s in InfluxDBQueryable<Sensor>.Queryable("my-bucket", "my-org", _client.GetQueryApi())
                where s.SensorId == "id-1"
                select s;

            var sensors = query.ToList();

            Assert.AreEqual(4, sensors.Count);
            foreach (var sensor in sensors)
            {
                Assert.AreEqual("id-1", sensor.SensorId);
            }
        }
        
        [Test]
        public void QueryWhereNothing()
        {
            var query = from s in InfluxDBQueryable<Sensor>.Queryable("my-bucket", "my-org", _client.GetQueryApi())
                where s.SensorId == "id-nothing"
                select s;

            var sensors = query.ToList();

            Assert.AreEqual(0, sensors.Count);
        }
        
        [Test]
        public void QueryOrderBy()
        {
            var query = from s in InfluxDBQueryable<Sensor>.Queryable("my-bucket", "my-org", _client.GetQueryApi())
                orderby s.Value
                select s;

            var sensors = query.ToList();

            Assert.AreEqual(12, sensors.First().Value);
            Assert.AreEqual(89, sensors.Last().Value);
        }
        
        [Test]
        public void QueryOrderByTime()
        {
            var query = from s in InfluxDBQueryable<Sensor>.Queryable("my-bucket", "my-org", _client.GetQueryApi())
                orderby s.Timestamp descending 
                select s;

            var sensors = query.ToList();

            Assert.AreEqual(new DateTime(2020, 11, 17, 8, 20, 15, DateTimeKind.Utc), 
                sensors.First().Timestamp);
            Assert.AreEqual(new DateTime(2020, 10, 15, 8, 20, 15, DateTimeKind.Utc), 
                sensors.Last().Timestamp);
        }

        [TearDown]
        protected void After()
        {
            _client.Dispose();
        }
    }
}