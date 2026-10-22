/*
 * Code Attribution:
 * The Table Storage calls (GetEntityIfExistsAsync, QueryAsync with a filter, AddEntityAsync, UpdateEntityAsync with an ETag,
 * DeleteEntityAsync) were adapted from the lecturer-provided StudentQueryEntity / Table Storage examples (IIE, 2026) and my
 * Part 1 and Part 2 Functions. Keeping all Users table code in one class follows the service-class idea in the
 * lecturer's Student Relations gateway example.
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 */

using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Gateway.Models;

namespace CoffeeNChill.Gateway.Services
{
    // The one place that reads and writes the Users table.
    public class UserStore
    {
        private const string TableName = "Users";

        private readonly TableServiceClient _tables;

        public UserStore(TableServiceClient tables)
        {
            _tables = tables;
        }

        // Gets the Users table and creates it if it is not there yet.
        private async Task<TableClient> GetTableAsync(CancellationToken ct = default)
        {
            TableClient table = _tables.GetTableClient(TableName);
            await table.CreateIfNotExistsAsync(ct);
            return table;
        }

        // Finds a user by email. The role is not known, so all three partitions are tried.
        // Returns null when nobody has that email.
        public async Task<UserEntity?> FindByEmailAsync(string email, CancellationToken ct = default)
        {
            TableClient table = await GetTableAsync(ct);
            string rowKey = email.Trim().ToLowerInvariant();

            foreach (string role in UserRoles.All)
            {
                var result = await table.GetEntityIfExistsAsync<UserEntity>(role, rowKey, cancellationToken: ct);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }

            return null;
        }

        // True when at least one Admin already exists.
        public async Task<bool> AdminExistsAsync(CancellationToken ct = default)
        {
            TableClient table = await GetTableAsync(ct);

            await foreach (var _ in table.QueryAsync<UserEntity>(u => u.PartitionKey == UserRoles.Admin, maxPerPage: 1, cancellationToken: ct))
            {
                return true;
            }

            return false;
        }

        // Saves a new user. Throws a RequestFailedException with status 409 if that role and email already exist.
        public async Task AddAsync(UserEntity user, CancellationToken ct = default)
        {
            TableClient table = await GetTableAsync(ct);
            await table.AddEntityAsync(user, ct);
        }

        // Lists every user, or only the users of one role.
        public async Task<List<UserEntity>> ListAsync(string? role = null, CancellationToken ct = default)
        {
            TableClient table = await GetTableAsync(ct);

            AsyncPageable<UserEntity> query = role is null
                ? table.QueryAsync<UserEntity>(cancellationToken: ct)
                : table.QueryAsync<UserEntity>(u => u.PartitionKey == role, cancellationToken: ct);

            var users = new List<UserEntity>();
            await foreach (UserEntity user in query)
            {
                users.Add(user);
            }

            return users;
        }

        // Saves changes to an existing user. The ETag makes Azure refuse (status 412) if the row changed in the meantime.
        public async Task UpdateAsync(UserEntity user, CancellationToken ct = default)
        {
            TableClient table = await GetTableAsync(ct);
            await table.UpdateEntityAsync(user, user.ETag, TableUpdateMode.Replace, ct);
        }

        // Deletes a user. Throws a RequestFailedException with status 404 if the row is already gone.
        public async Task DeleteAsync(UserEntity user, CancellationToken ct = default)
        {
            TableClient table = await GetTableAsync(ct);
            await table.DeleteEntityAsync(user.PartitionKey, user.RowKey, cancellationToken: ct);
        }
    }
}