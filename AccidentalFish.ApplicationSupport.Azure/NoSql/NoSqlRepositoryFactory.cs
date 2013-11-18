﻿using AccidentalFish.ApplicationSupport.Core.Configuration;
using AccidentalFish.ApplicationSupport.Core.NoSql;
using CuttingEdge.Conditions;

namespace AccidentalFish.ApplicationSupport.Azure.NoSql
{
    internal class NoSqlRepositoryFactory : INoSqlRepositoryFactory
    {
        private readonly IConfiguration _configuration;

        public NoSqlRepositoryFactory(IConfiguration configuration)
        {
            Condition.Requires(configuration).IsNotNull();
            _configuration = configuration;
        }

        public IAsynchronousNoSqlRepository<T> CreateAsynchronousNoSqlRepository<T>(
            string storageAccountConnectionString,
            string tableName) where T : NoSqlEntity, new()
        {
            Condition.Requires(storageAccountConnectionString).IsNotNullOrWhiteSpace();
            Condition.Requires(tableName).IsNotNullOrWhiteSpace();
            return new AsynchronousNoSqlRepository<T>(storageAccountConnectionString, tableName);
        }

        public IAsynchronousNoSqlRepository<T> CreateAsynchronousNoSqlRepository<T>(string tableName) where T : NoSqlEntity, new()
        {
            Condition.Requires(tableName).IsNotNullOrWhiteSpace();
            return new AsynchronousNoSqlRepository<T>(_configuration.StorageAccountConnectionString, tableName);
        }
    }
}