﻿using System.Collections.Generic;
using System.Linq;
using Cassandra.Data.Linq;
using Cassandra.IntegrationTests.TestBase;
using Cassandra.Mapping;
using NUnit.Framework;

namespace Cassandra.IntegrationTests.Mapping.Tests
{
    [Category("short")]
    public class CqlClientTest : TestGlobals
    {
        ISession _session = null;
        private readonly Logger _logger = new Logger(typeof(Attributes));
        string _uniqueKsName;

        [SetUp]
        public void SetupTest()
        {
            _session = TestClusterManager.GetTestCluster(1).Session;
            _uniqueKsName = TestUtils.GetUniqueKeyspaceName();
            _session.CreateKeyspace(_uniqueKsName);
            _session.ChangeKeyspace(_uniqueKsName);
        }

        [TearDown]
        public void TeardownTest()
        {
            _session.DeleteKeyspace(_uniqueKsName);
        }

        /// <summary>
        /// Verify that two separate instances of the CqlClient object can co-exist
        /// </summary>
        [Test]
        public void CqlClient_TwoInstancesBasedOnSameSession()
        {
            // Setup
            var table1 = _session.GetTable<Poco1>();
            table1.Create();
            string cqlSelectAll1 = "SELECT * from " + table1.Name;
            var table2 = _session.GetTable<Poco2>();
            table2.Create();
            string cqlSelectAll2 = "SELECT * from " + table2.Name;

            // Now re-instantiate the cqlClient, but with mapping rule that resolves the missing key issue
            var cqlClient1 = new Mapper(_session, new MappingConfiguration().Define(new Poco1Mapping()));
            var cqlClient2 = new Mapper(_session, new MappingConfiguration().Define(new Poco2Mapping()));

            // insert new record into two separate tables
            Poco1 poco1 = new Poco1();
            poco1.SomeString1 += "1";
            cqlClient1.Insert(poco1);

            Poco2 poco2 = new Poco2();
            poco2.SomeString2 += "1";
            cqlClient2.Insert(poco2);

            // Select Values from each table
            List<Poco1> poco1s = cqlClient1.Fetch<Poco1>(cqlSelectAll1).ToList();
            Assert.AreEqual(1, poco1s.Count);
            Assert.AreEqual(poco1.SomeString1, poco1s[0].SomeString1);
            Assert.AreEqual(poco1.SomeDouble1, poco1s[0].SomeDouble1);

            List<Poco2> poco2s = cqlClient2.Fetch<Poco2>(cqlSelectAll2).ToList();
            Assert.AreEqual(1, poco2s.Count);
            Assert.AreEqual(poco2.SomeString2, poco2s[0].SomeString2);
            Assert.AreEqual(poco2.SomeDouble2, poco2s[0].SomeDouble2);

            // Try that again
            poco1s.Clear();
            Assert.AreEqual(0, poco1s.Count);
            poco1s = cqlClient1.Fetch<Poco1>(cqlSelectAll1).ToList();
            Assert.AreEqual(1, poco1s.Count);
            Assert.AreEqual(poco1.SomeString1, poco1s[0].SomeString1);
            Assert.AreEqual(poco1.SomeDouble1, poco1s[0].SomeDouble1);

            poco2s.Clear();
            Assert.AreEqual(0, poco2s.Count);
            poco2s = cqlClient1.Fetch<Poco2>(cqlSelectAll2).ToList();
            Assert.AreEqual(1, poco2s.Count);
            Assert.AreEqual(poco2.SomeString2, poco2s[0].SomeString2);
            Assert.AreEqual(poco2.SomeDouble2, poco2s[0].SomeDouble2);

        }

        ////////////////////////////////////////////////////
        /// Test Classes
        ////////////////////////////////////////////////////

        [Cassandra.Data.Linq.Table("poco1")]
        private class Poco1
        {
            [Cassandra.Data.Linq.PartitionKeyAttribute]
            [Cassandra.Mapping.Attributes.PartitionKey]
            [Cassandra.Data.Linq.Column("somestring1")]
            public string SomeString1 = "somevalue_1_";

            [Cassandra.Data.Linq.Column("somedouble1")]
            public double SomeDouble1 = 1;
        }

        [Cassandra.Data.Linq.Table("poco2")]
        private class Poco2
        {
            [Cassandra.Data.Linq.PartitionKeyAttribute]
            [Cassandra.Mapping.Attributes.PartitionKey]
            [Cassandra.Data.Linq.Column("somestring2")]
            public string SomeString2 = "somevalue_2_";

            [Cassandra.Data.Linq.Column("somedouble2")]
            public double SomeDouble2 = 2;
        }

        class Poco1Mapping : Map<Poco1>
        {
            public Poco1Mapping()
            {
                TableName("poco1");
                PartitionKey(u => u.SomeString1);
                Column(u => u.SomeString1, cm => cm.WithName("somestring1"));
            }
        }

        class Poco2Mapping : Map<Poco2>
        {
            public Poco2Mapping()
            {
                TableName("poco2");
                PartitionKey(u => u.SomeString2);
                Column(u => u.SomeString2, cm => cm.WithName("somestring2"));
            }
        }



    }
}
