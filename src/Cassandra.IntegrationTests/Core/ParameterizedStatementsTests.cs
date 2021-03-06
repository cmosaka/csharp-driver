//
//      Copyright (C) 2012-2014 DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//

﻿using System.Linq;
﻿using Cassandra.IntegrationTests.TestBase;
﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Cassandra.IntegrationTests.Core
{
    [Category("short")]
    [TestCassandraVersion(2, 0)]
    public class ParameterizedStatementsTests : TestGlobals
    {
        ISession _session = null;
        private int lastNodeCountUsed = 1;

        [SetUp]
        public void Setup()
        {
            _session = TestClusterManager.GetTestCluster(1).Session;

            try
            {
                _session.WaitForSchemaAgreement(_session.Execute(String.Format(TestUtils.CreateTableAllTypes, AllTypesTableName)));
            }
            catch (Cassandra.AlreadyExistsException e) { }
        }

        private const string AllTypesTableName = "all_types_table_queryparams";

        [Test]
        public void CollectionParamsTests()
        {
            var id = Guid.NewGuid();
            var map = new SortedDictionary<string, string> { { "fruit", "apple" }, { "band", "Beatles" } };
            var list = new List<string> { "one", "two" };
            var set = new List<string> { "set_1one", "set_2two" };

            var insertStatement = new SimpleStatement(String.Format("INSERT INTO {0} (id, map_sample, list_sample, set_sample) VALUES (?, ?, ?, ?)", AllTypesTableName));
            _session.Execute(insertStatement.Bind(id, map, list, set));
            var row = _session.Execute(new SimpleStatement(String.Format("SELECT * FROM {0} WHERE id = ?", AllTypesTableName)).Bind(id)).First();
            CollectionAssert.AreEquivalent(map, row.GetValue<IDictionary<string, string>>("map_sample"));
            CollectionAssert.AreEquivalent(list, row.GetValue<List<string>>("list_sample"));
            CollectionAssert.AreEquivalent(set, row.GetValue<List<string>>("set_sample"));
        }

        [Test]
        [TestCassandraVersion(2, 1)]
        public void SimpleStatementSetTimestamp()
        {
            var timestamp = new DateTimeOffset(1999, 12, 31, 1, 2, 3, TimeSpan.Zero);
            var id = Guid.NewGuid();
            var insertStatement = new SimpleStatement(String.Format("INSERT INTO {0} (id, text_sample) VALUES (?, ?)", AllTypesTableName));
            _session.Execute(insertStatement.Bind(id, "sample text").SetTimestamp(timestamp));
            var row = _session.Execute(new SimpleStatement(String.Format("SELECT id, text_sample, writetime(text_sample) FROM {0} WHERE id = ?", AllTypesTableName)).Bind(id)).First();
            Assert.NotNull(row.GetValue<string>("text_sample"));
            Assert.AreEqual(TypeCodec.ToUnixTime(timestamp).Ticks / 10, row.GetValue<object>("writetime(text_sample)"));
        }

        [Test]
        [TestCassandraVersion(2, 1)]
        public void SimpleStatementNamedValues()
        {
            var insertQuery = String.Format("INSERT INTO {0} (text_sample, int_sample, bigint_sample, id) VALUES (:my_text, :my_int, :my_bigint, :my_id)", AllTypesTableName);
            var statement = new SimpleStatement(insertQuery);

            var id = Guid.NewGuid();
            _session.Execute(
                statement.Bind(
                    new { my_int = 100, my_bigint = -500L, my_id = id, my_text = "named params ftw again!" }));

            var row = _session.Execute(String.Format("SELECT int_sample, bigint_sample, text_sample FROM {0} WHERE id = {1:D}", AllTypesTableName, id)).First();

            Assert.AreEqual(100, row.GetValue<int>("int_sample"));
            Assert.AreEqual(-500L, row.GetValue<long>("bigint_sample"));
            Assert.AreEqual("named params ftw again!", row.GetValue<string>("text_sample"));
        }

        [Test]
        [TestCassandraVersion(2, 1)]
        public void SimpleStatementNamedValuesCaseInsensitivity()
        {
            var insertQuery = String.Format("INSERT INTO {0} (id, \"text_sample\", int_sample) VALUES (:my_ID, :my_TEXT, :MY_INT)", AllTypesTableName);
            var statement = new SimpleStatement(insertQuery);

            var id = Guid.NewGuid();
            _session.Execute(
                statement.Bind(
                    new { my_INt = 1, my_TEXT = "WAT1", my_id = id}));

            var row = _session.Execute(String.Format("SELECT * FROM {0} WHERE id = {1:D}", AllTypesTableName, id)).First();
            Assert.AreEqual(1, row.GetValue<int>("int_sample"));
            Assert.AreEqual("WAT1", row.GetValue<string>("text_sample"));
        }

        [Test]
        [TestCassandraVersion(2, 1)]
        public void SimpleStatementNamedValuesNotSpecified()
        {
            var insertQuery = String.Format("INSERT INTO {0} (float_sample, text_sample, bigint_sample, id) VALUES (:MY_float, :my_TexT, :my_BIGint, :id)", AllTypesTableName);
            var statement = new SimpleStatement(insertQuery);

            Assert.Throws<InvalidQueryException>(() => _session.Execute(
                statement.Bind(
                    new {id = Guid.NewGuid(), my_bigint = 1L })));
        }

        [Test]
        public void Text()
        {
            ParameterizedStatement(typeof(string));
        }

        [Test]
        public void Blob()
        {
            ParameterizedStatement(typeof(byte));
        }

        [Test]
        public void ASCII()
        {
            ParameterizedStatement(typeof(Char));
        }

        [Test]
        public void Decimal()
        {
            ParameterizedStatement(typeof(Decimal));
        }

        [Test]
        public void VarInt()
        {
            ParameterizedStatement(typeof(BigInteger));
        }

        [Test]
        public void BigInt()
        {
            ParameterizedStatement(typeof(Int64));
        }

        [Test]
        public void Double()
        {
            ParameterizedStatement(typeof(Double));
        }

        [Test]
        public void Float()
        {
            ParameterizedStatement(typeof(Single));
        }

        [Test]
        public void Int()
        {
            ParameterizedStatement(typeof(Int32));
        }

        [Test]
        public void Boolean()
        {
            ParameterizedStatement(typeof(Boolean));
        }

        [Test]
        public void UUID()
        {
            ParameterizedStatement(typeof(Guid));
        }

        [Test]
        public void TimeStamp()
        {
            ParameterizedStatementTimeStamp();
        }

        [Test]
        public void IntAsync()
        {
            ParameterizedStatement(typeof(Int32), true);
        }

        private void ParameterizedStatementTimeStamp()
        {
            RowSet rs = null;
            var expectedValues = new List<object[]>(1);
            var tableName = "table" + Guid.NewGuid().ToString("N").ToLower();
            var valuesToTest = new List<object[]> { new object[] { Guid.NewGuid(), new DateTimeOffset(2011, 2, 3, 16, 5, 0, new TimeSpan(0000)) },
                                                    {new object[] {Guid.NewGuid(), (long)0}}};

            foreach (var bindValues in valuesToTest)
            {
                expectedValues.Add(bindValues);

                CreateTable(tableName, "timestamp");

                SimpleStatement statement = new SimpleStatement(String.Format("INSERT INTO {0} (id, val) VALUES (?, ?)", tableName));
                statement.Bind(bindValues);

                _session.Execute(statement);

                // Verify results
                rs = _session.Execute("SELECT * FROM " + tableName);

                VerifyData(rs, expectedValues);

                DropTable(tableName);

                expectedValues.Clear();
            }
        }

        private void ParameterizedStatement(Type type, bool testAsync = false)
        {
            var tableName = "table" + Guid.NewGuid().ToString("N").ToLower();
            var cassandraDataTypeName = QueryTools.convertTypeNameToCassandraEquivalent(type);
            var expectedValues = new List<object[]>(1);
            var val = Randomm.RandomVal(type);
            var bindValues = new object[] { Guid.NewGuid(), val };
            expectedValues.Add(bindValues);
            
            CreateTable(tableName, cassandraDataTypeName);

            SimpleStatement statement = new SimpleStatement(String.Format("INSERT INTO {0} (id, val) VALUES (?, ?)", tableName));
            statement.Bind(bindValues);

            if (testAsync)
            {
                _session.ExecuteAsync(statement).Wait(500);
            }
            else
            {
                _session.Execute(statement);
            }

            // Verify results
            RowSet rs = _session.Execute("SELECT * FROM " + tableName);
            VerifyData(rs, expectedValues);

        }

        private void CreateTable(string tableName, string type)
        {
            try
            {
                QueryTools.ExecuteSyncNonQuery(_session, string.Format(@"CREATE TABLE {0}(
                                                                        id uuid PRIMARY KEY,
                                                                        val {1}
                                                                        );", tableName, type));
            }
            catch (AlreadyExistsException)
            {
            }
        }

        private void DropTable(string tableName)
        {
            QueryTools.ExecuteSyncNonQuery(_session, string.Format(@"DROP TABLE {0};", tableName));
        }

        private static DateTimeOffset FromUnixTime(long unixTime)
        {
            var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan(0000));
            return epoch.AddSeconds(unixTime);
        }

        private static void VerifyData(RowSet rowSet, List<object[]> expectedValues)
        {
            int x = 0;
            foreach (Row row in rowSet.GetRows())
            {
                int y = 0;
                object[] objArr = expectedValues[x];

                var rowEnum = row.GetEnumerator();
                while (rowEnum.MoveNext())
                {
                    var current = rowEnum.Current;
                    if (objArr[y].GetType() == typeof(byte[]))
                    {
                        Assert.AreEqual((byte[])objArr[y], (byte[])current);
                    }
                    else if (current.GetType() == typeof(DateTimeOffset))
                    {
                        if (objArr[y].GetType() == typeof(long))
                        {
                            if ((long)objArr[y] == 0)
                            {
                                Assert.True(current.ToString() == "1/1/1970 12:00:00 AM +00:00");
                            }
                            else
                            {
                                Assert.AreEqual(FromUnixTime((long)objArr[y]), (DateTimeOffset)current, String.Format("Found difference between expected and actual row {0} != {1}", objArr[y].ToString(), current.ToString()));
                            }
                        }
                        else
                        {
                            Assert.AreEqual((DateTimeOffset)objArr[y], ((DateTimeOffset)current), String.Format("Found difference between expected and actual row {0} != {1}", objArr[y].ToString(), current.ToString()));
                        }
                    }
                    else
                    {
                        Assert.True(objArr[y].Equals(current), String.Format("Found difference between expected and actual row {0} != {1}", objArr[y].ToString(), current.ToString()));
                    }
                    y++;
                }

                x++;
            }
        }
    }
}
