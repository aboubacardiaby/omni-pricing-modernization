namespace Pricing.IntegrationTests;

using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Pricing.Infrastructure.Db2;
using Xunit;

public sealed class Db2InfrastructureTests
{
    [Fact]
    public async Task DapperExecutorBindsParametersWithoutPuttingValuesInSqlOrTrace()
    {
        var connection = new RecordingConnection();
        using var listener = CreateListener(out List<Activity> activities);
        var executor = CreateExecutor(new StubConnectionFactory(connection));

        int? result = await executor.QuerySingleOrDefaultAsync<int>(
            new Db2Query("product.lookup", "SELECT RESULT FROM PRODUCT WHERE ID = @Id"),
            new { Id = "sensitive-product" },
            CancellationToken.None);

        Assert.Equal(1, result);
        Assert.DoesNotContain("sensitive-product", connection.LastCommand!.CommandText, StringComparison.Ordinal);
        DbParameter parameter = Assert.Single(connection.LastCommand.Parameters.Cast<DbParameter>());
        Assert.Equal("Id", parameter.ParameterName);
        Assert.Equal("sensitive-product", parameter.Value);
        Activity activity = Assert.Single(activities);
        Assert.Equal("db2", activity.GetTagItem("db.system.name"));
        Assert.Equal("product.lookup", activity.GetTagItem("db.operation.name"));
        Assert.DoesNotContain(activity.TagObjects, tag =>
            tag.Value?.ToString()?.Contains("sensitive-product", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task TransientDb2FailureIsMappedWithoutLeakingSql()
    {
        var exception = new TestDbException("deadlock detail", "40001", -911);
        var connection = new RecordingConnection(exception);
        var executor = CreateExecutor(new StubConnectionFactory(connection));

        Db2AccessException mapped = await Assert.ThrowsAsync<Db2AccessException>(async () =>
            await executor.QuerySingleOrDefaultAsync<int>(
                new Db2Query("customer.lookup", "SELECT RESULT FROM CUSTOMER WHERE ID = @Id"),
                new { Id = 123 },
                CancellationToken.None));

        Assert.True(mapped.IsTransient);
        Assert.Equal("40001", mapped.SqlState);
        Assert.Equal(-911, mapped.ProviderErrorCode);
        Assert.Equal("customer.lookup", mapped.Operation);
        Assert.DoesNotContain("SELECT", mapped.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancellationPropagatesBeforeOpeningConnection()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var factory = new StubConnectionFactory(new RecordingConnection());

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await CreateExecutor(factory).QueryAsync<int>(
                new Db2Query("cancelled.lookup", "SELECT RESULT FROM PRODUCT"),
                null,
                cancellation.Token));
        Assert.Equal(1, factory.CallCount);
    }

    [Fact]
    public async Task HealthCheckExecutesDb2Probe()
    {
        var connection = new RecordingConnection();
        var healthCheck = new Db2ConnectionHealthCheck(new StubConnectionFactory(connection));

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("SELECT 1 FROM SYSIBM.SYSDUMMY1", connection.LastCommand!.CommandText);
    }

    private static DapperDb2QueryExecutor CreateExecutor(IDb2ConnectionFactory factory) =>
        new(factory, Options.Create(new Db2Options
        {
            ProviderInvariantName = "test",
            ConnectionString = "sanitized",
            CommandTimeoutSeconds = 15,
        }));

    private static ActivityListener CreateListener(out List<Activity> activities)
    {
        activities = [];
        List<Activity> captured = activities;
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DapperDb2QueryExecutor.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = captured.Add,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private sealed class StubConnectionFactory(DbConnection connection) : IDb2ConnectionFactory
    {
        internal int CallCount { get; private set; }

        public ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(connection);
        }
    }

    private sealed class RecordingConnection(Exception? executionFailure = null) : DbConnection
    {
        private ConnectionState state = ConnectionState.Open;

        public RecordingCommand? LastCommand { get; private set; }
        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "TEST";
        public override string DataSource => "SANITIZED";
        public override string ServerVersion => "0";
        public override ConnectionState State => state;
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public override void Close() => state = ConnectionState.Closed;
        public override void Open() => state = ConnectionState.Open;
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            throw new NotSupportedException();

        protected override DbCommand CreateDbCommand()
        {
            LastCommand = new RecordingCommand(this, executionFailure);
            return LastCommand;
        }
    }

    private sealed class RecordingCommand(DbConnection connection, Exception? executionFailure) : DbCommand
    {
        private readonly RecordingParameterCollection parameters = new();

        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        [AllowNull]
        protected override DbConnection DbConnection { get; set; } = connection;
        protected override DbParameterCollection DbParameterCollection => parameters;
        protected override DbTransaction? DbTransaction { get; set; }
        public override void Cancel() { }
        public override int ExecuteNonQuery() => 1;
        public override object ExecuteScalar() => ExecuteResult();
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new RecordingParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            _ = ExecuteResult();
            var table = new DataTable();
            table.Columns.Add("RESULT", typeof(int));
            table.Rows.Add(1);
            return table.CreateDataReader();
        }

        private int ExecuteResult()
        {
            if (executionFailure is not null)
            {
                throw executionFailure;
            }

            return 1;
        }
    }

    private sealed class RecordingParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public override bool IsNullable { get; set; }
        [AllowNull]
        public override string ParameterName { get; set; } = string.Empty;
        [AllowNull]
        public override string SourceColumn { get; set; } = string.Empty;
        public override object? Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }
        public override void ResetDbType() { }
    }

    private sealed class RecordingParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> values = [];

        public override int Count => values.Count;
        public override object SyncRoot => ((ICollection)values).SyncRoot;
        public override int Add(object value)
        {
            values.Add((DbParameter)value);
            return values.Count - 1;
        }

        public override void AddRange(Array valuesToAdd)
        {
            foreach (object value in valuesToAdd)
            {
                Add(value);
            }
        }

        public override void Clear() => values.Clear();
        public override bool Contains(object value) => values.Contains((DbParameter)value);
        public override bool Contains(string value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, int index) => ((ICollection)values).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => values.GetEnumerator();
        public override int IndexOf(object value) => values.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) =>
            values.FindIndex(parameter => parameter.ParameterName == parameterName);
        public override void Insert(int index, object value) => values.Insert(index, (DbParameter)value);
        public override void Remove(object value) => values.Remove((DbParameter)value);
        public override void RemoveAt(int index) => values.RemoveAt(index);
        public override void RemoveAt(string parameterName) => values.RemoveAt(IndexOf(parameterName));
        protected override DbParameter GetParameter(int index) => values[index];
        protected override DbParameter GetParameter(string parameterName) => values[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => values[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            int index = IndexOf(parameterName);
            if (index < 0)
            {
                values.Add(value);
            }
            else
            {
                values[index] = value;
            }
        }
    }

    private sealed class TestDbException(string message, string sqlState, int errorCode) : DbException(message, errorCode)
    {
        public override string? SqlState => sqlState;
    }
}
