// Copyright (C) 2024 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of dkg service node
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
// TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS
// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using Npgsql;

namespace TrustVpn.Data;
public class DbEnsure
{
    readonly static string sqlScript_0_1_4 = @"
            START TRANSACTION;

            DROP TABLE IF EXISTS ""users"";
            DROP TABLE IF EXISTS ""profiles"";
            DROP TABLE IF EXISTS ""versions"";

            CREATE TABLE ""profiles"" (
              ""id""              SERIAL PRIMARY KEY,
              ""name""            VARCHAR(64) NOT NULL,
              ""profile""         VARCHAR(64) NOT NULL
            );

            -- 'blocked' - зарезервированное значение.Не менять, не удалять, должно быть на первой позиции (id= 1).
            INSERT INTO ""profiles"" (""name"", ""profile"") VALUES('Блокировка', 'blocked');
                    INSERT INTO ""profiles"" (""name"", ""profile"") VALUES('Ограниченный трафик', 'limited');
                    INSERT INTO ""profiles"" (""name"", ""profile"") VALUES('Неoграниченный трафик', 'unlimited');

            CREATE TABLE ""users"" (
              ""id""              SERIAL PRIMARY KEY,
              ""first_name""      VARCHAR(16) NOT NULL,
              ""last_name""       VARCHAR(16) NOT NULL,
              ""patronimic""      VARCHAR(16) NOT NULL,
              ""email""           VARCHAR(64) NOT NULL,
              ""password""        VARCHAR(64) NOT NULL,
              ""is_admin""        BOOLEAN NOT NULL DEFAULT FALSE,
              ""profile_id""      INTEGER NOT NULL DEFAULT 0 REFERENCES ""profiles"" (""id"") ON DELETE RESTRICT
            );

            CREATE UNIQUE INDEX ""idx_users_email"" ON ""users"" (""email"");

            -- password: Ivanov$123
            INSERT INTO ""users"" (""first_name"", ""patronimic"", ""last_name"", ""email"", ""password"", ""is_admin"", ""profile_id"") VALUES
            ('Иван', 'Иванович', 'Иванов', 'ivanov@example.com', '$2a$11$GAYD9aArfam2L/wxy26r2.gwNSTgaqBN9jZrXsDkc7stvHMYt12XW', TRUE, 1);

             CREATE TABLE ""versions"" (
              ""id""      SERIAL PRIMARY KEY,
              ""version"" VARCHAR(16) NOT NULL,
              ""date""    DATE NOT NULL
            );

            CREATE UNIQUE INDEX ""idx_versions_version"" ON ""versions"" (""version"");

            -- Всё, что было до версии 0.1.3 не имеет даже исторической ценности :)
            INSERT INTO ""versions"" (""version"", ""date"") VALUES('0.1.3', '2023-12-02');
            INSERT INTO ""versions"" (""version"", ""date"") VALUES('0.1.4', '2023-12-05');

            COMMIT;
        ";

    private readonly ILogger<DbEnsure> _logger;
    public DbEnsure(ILogger<DbEnsure> logger)
    {
        _logger = logger;
    }
    private static string PuVersionUpdateQuery(string v)
    {
        return @"
            START TRANSACTION;
            INSERT INTO ""versions"" (""version"", ""date"") VALUES
            ('" + v + "', '" + DateTime.Now.ToString("yyyy-MM-dd") + @"');
            COMMIT;
            ";
    }
    private static string VQuery(string v)
    {
        return $"SELECT COUNT(*) FROM versions WHERE version = '{v}';";
    }

    private static bool VCheck(string v, NpgsqlConnection connection)
    {
        var command = new NpgsqlCommand(VQuery(v), connection);
        var rows = command.ExecuteScalar();
        return (rows != null && (long)rows != 0);
    }

    public static int Ensure_0_1_4(NpgsqlConnection connection)
    {
        // Check if table 'versions' exists
        var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'versions';";
        var command = new NpgsqlCommand(sql, connection);
        var rows = command.ExecuteScalar();

        int r = 0;

        if (rows != null && (long)rows != 0)
        {
            sql = "SELECT COUNT(*) FROM versions WHERE version = '0.1.4';";
            command = new NpgsqlCommand(sql, connection);
            rows = command.ExecuteScalar();
        }

        if (rows == null || (long)rows == 0)
        {
            var scriptCommand = new NpgsqlCommand(sqlScript_0_1_4, connection);
            r = scriptCommand.ExecuteNonQuery();
        }

        return r;
    }
    private static void PuVersionUpdate(string v, NpgsqlConnection connection)
    {
        if (!VCheck(v, connection))
        {
            var scriptCommand = new NpgsqlCommand(PuVersionUpdateQuery(v), connection);
            scriptCommand.ExecuteNonQuery();
        }
    }
    public static void EnsureVersion(string v, string s, NpgsqlConnection connection)
    {
        if (!VCheck(v, connection))
        {
            var scriptCommand = new NpgsqlCommand(s, connection);
            scriptCommand.ExecuteNonQuery();
        }
    }
    public void Ensure(NpgsqlConnection connection)
    {
        try
        {
            _logger.LogInformation("Initializing database at 0.1.4");
            Ensure_0_1_4(connection);
            _logger.LogInformation("Database version 0.2.1");
            PuVersionUpdate("0.2.1", connection);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error initializing database: {msg}", ex.Message);
        }
    }
}



