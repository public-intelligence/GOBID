using GOBID.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GOBID.Infrastructure.Data
{
    public class SecondaryDbConnection : ISecondaryDbConnection
    {
        private readonly SqlConnection _connection;

        public SecondaryDbConnection(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
        }

        public IDbTransaction BeginTransaction()
        {
            return _connection.BeginTransaction();
        }

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return _connection.BeginTransaction(isolationLevel);
        }

        public void ChangeDatabase(string databaseName)
        {
            _connection.ChangeDatabase(databaseName);
        }

        public void Close()
        {
            _connection.Close();
        }

        public IDbCommand CreateCommand()
        {
            return _connection.CreateCommand();
        }

        public void Open()
        {
            _connection.Open();
        }

        public string? ConnectionString
        {
#pragma warning disable CS8766
            get => _connection.ConnectionString;
#pragma warning restore CS8766
            set => _connection.ConnectionString = value!;
        }

        public int ConnectionTimeout => _connection.ConnectionTimeout;

        public string Database => _connection.Database;

        public ConnectionState State => _connection.State;

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
