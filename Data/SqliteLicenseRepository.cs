using KeyAuthDesktopPanel.Models;
using Microsoft.Data.Sqlite;

namespace KeyAuthDesktopPanel.Data;

public sealed class SqliteLicenseRepository
{
    private readonly string _connectionString;

    public SqliteLicenseRepository()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KeyAuthDesktopPanel"
        );
        Directory.CreateDirectory(appData);

        var dbPath = Path.Combine(appData, "licenses.db");
        _connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS licenses (
                id TEXT PRIMARY KEY,
                key_value TEXT NOT NULL UNIQUE,
                app_name TEXT NOT NULL,
                buyer TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                expires_at_utc TEXT NOT NULL,
                active INTEGER NOT NULL,
                activations INTEGER NOT NULL,
                last_validated_at_utc TEXT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public void Insert(LicenseRecord license)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO licenses (
                id, key_value, app_name, buyer, created_at_utc, expires_at_utc, active, activations, last_validated_at_utc
            ) VALUES (
                $id, $key, $appName, $buyer, $createdAtUtc, $expiresAtUtc, $active, $activations, $lastValidatedAtUtc
            );
            """;
        command.Parameters.AddWithValue("$id", license.Id);
        command.Parameters.AddWithValue("$key", license.Key);
        command.Parameters.AddWithValue("$appName", license.AppName);
        command.Parameters.AddWithValue("$buyer", license.Buyer);
        command.Parameters.AddWithValue("$createdAtUtc", license.CreatedAtUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$expiresAtUtc", license.ExpiresAtUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$active", license.Active ? 1 : 0);
        command.Parameters.AddWithValue("$activations", license.Activations);
        command.Parameters.AddWithValue(
            "$lastValidatedAtUtc",
            license.LastValidatedAtUtc?.UtcDateTime.ToString("O") ?? (object)DBNull.Value
        );
        command.ExecuteNonQuery();
    }

    public List<LicenseRecord> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                id, key_value, app_name, buyer, created_at_utc, expires_at_utc, active, activations, last_validated_at_utc
            FROM licenses
            ORDER BY created_at_utc DESC;
            """;

        using var reader = command.ExecuteReader();
        var list = new List<LicenseRecord>();
        while (reader.Read())
        {
            list.Add(Map(reader));
        }

        return list;
    }

    public LicenseRecord? FindByKey(string key)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                id, key_value, app_name, buyer, created_at_utc, expires_at_utc, active, activations, last_validated_at_utc
            FROM licenses
            WHERE key_value = $key
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$key", key);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return Map(reader);
    }

    public void Revoke(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE licenses SET active = 0 WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public void Delete(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM licenses WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public void ClearAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM licenses;";
        command.ExecuteNonQuery();
    }

    public void RegisterActivation(string id, int nextActivationCount)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE licenses
            SET activations = $activations, last_validated_at_utc = $lastValidatedAtUtc
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$activations", nextActivationCount);
        command.Parameters.AddWithValue("$lastValidatedAtUtc", DateTime.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    private static LicenseRecord Map(SqliteDataReader reader)
    {
        var created = DateTimeOffset.Parse(reader.GetString(4));
        var expires = DateTimeOffset.Parse(reader.GetString(5));

        DateTimeOffset? lastValidated = null;
        if (!reader.IsDBNull(8))
        {
            lastValidated = DateTimeOffset.Parse(reader.GetString(8));
        }

        return new LicenseRecord
        {
            Id = reader.GetString(0),
            Key = reader.GetString(1),
            AppName = reader.GetString(2),
            Buyer = reader.GetString(3),
            CreatedAtUtc = created,
            ExpiresAtUtc = expires,
            Active = reader.GetInt64(6) == 1,
            Activations = reader.GetInt32(7),
            LastValidatedAtUtc = lastValidated
        };
    }
}
