﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccidentalFish.ApplicationSupport.Core.NoSql;
using CuttingEdge.Conditions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;

namespace AccidentalFish.ApplicationSupport.Azure.NoSql
{
    internal class AsynchronousNoSqlRepository<T> : IAsynchronousNoSqlRepository<T> where T : NoSqlEntity, new()
    {
        private readonly CloudTable _table;

        public AsynchronousNoSqlRepository(string connectionString, string tableName)
        {
            Condition.Requires(tableName).IsNotNullOrWhiteSpace();
            Condition.Requires(connectionString).IsNotNullOrWhiteSpace();

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            tableClient.RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(120), 3);
            _table = tableClient.GetTableReference(tableName);            
        }

        public Task InsertAsync(T item)
        {
// This is disabled to allow someone outside the solution to derive from NoSqlEntity and add a ITableEntity to improve performance
// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable ExpressionIsAlwaysNull
            ITableEntity tableEntity = item as ITableEntity;
// ReSharper restore ExpressionIsAlwaysNull
// ReSharper restore SuspiciousTypeConversion.Global
// ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (tableEntity == null)
// ReSharper restore ConditionIsAlwaysTrueOrFalse
            {
                tableEntity = new AzureNoSqlEntityWrapper<T>(item);
            }
            TableOperation operation = TableOperation.Insert(tableEntity);
            return _table.ExecuteAsync(operation);
        }

        public Task InsertBatchAsync(IEnumerable<T> items)
        {
            TableBatchOperation operation = new TableBatchOperation();
            foreach (T item in items)
            {
// This is disabled to allow someone outside the solution to derive from NoSqlEntity and add a ITableEntity to improve performance
// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable ExpressionIsAlwaysNull
                ITableEntity tableEntity = item as ITableEntity;
// ReSharper restore ExpressionIsAlwaysNull
// ReSharper restore SuspiciousTypeConversion.Global
// ReSharper disable ConditionIsAlwaysTrueOrFalse
                if (tableEntity == null)
// ReSharper restore ConditionIsAlwaysTrueOrFalse
                {
                    tableEntity = new AzureNoSqlEntityWrapper<T>(item);
                }

                operation.Insert(tableEntity);
            }

            return _table.ExecuteBatchAsync(operation);
        }

        public Task<T> GetAsync(string partitionKey, string rowKey)
        {
            return Task.Factory.StartNew(() =>
            {
                TableOperation operation = TableOperation.Retrieve<T>(partitionKey, rowKey);
                TableResult result = _table.Execute(operation);
                return (T)result.Result;
            });
        }

        public Task<IEnumerable<T>> GetAsync(string partitionKey)
        {
            return Task.Factory.StartNew(() =>
            {
                TableQuery<T> query =
                    new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                        partitionKey));
                return (IEnumerable<T>)_table.ExecuteQuery(query).ToList();
            });
        }

        public Task<IEnumerable<T>> GetAsync(string partitionKey, int take)
        {
            return Task.Factory.StartNew(() =>
            {
                TableQuery<T> query =
                    new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                        partitionKey)).Take(take);
                return (IEnumerable<T>)_table.ExecuteQuery(query).ToList();
            });
        }

        public Task InsertOrUpdateAsync(T item)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(T item)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(T item)
        {
            TableOperation operation = TableOperation.Replace(item);
            return _table.ExecuteAsync(operation);
        }

        public async Task<IEnumerable<T>> QueryAsync(string column, string value)
        {
            List<T> results = new List<T>();
            TableQuery<T> query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition(column, QueryComparisons.Equal, value));
            TableQuerySegment<T> querySegment = null;

            while (querySegment == null || querySegment.ContinuationToken != null)
            {
                querySegment = await _table.ExecuteQuerySegmentedAsync(query, querySegment != null ? querySegment.ContinuationToken : null);
                results.AddRange(querySegment.Results);
            }
            return results;
        }
    }
}