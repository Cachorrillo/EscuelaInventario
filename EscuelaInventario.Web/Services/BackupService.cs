using Microsoft.Data.SqlClient;

namespace EscuelaInventario.Web.Services;

public class BackupService
{
    private readonly string _connectionString;

    public BackupService(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("EscuelaInventarioConnection")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión EscuelaInventarioConnection.");
    }

    public async Task<ResultadoBackup> CrearBackupAsync()
    {
        var connectionStringBuilder =
            new SqlConnectionStringBuilder(_connectionString);

        string nombreBaseDatos =
            connectionStringBuilder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(nombreBaseDatos))
        {
            throw new InvalidOperationException(
                "La cadena de conexión no indica el nombre de la base de datos.");
        }

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        string? carpetaBackup;

        await using (var commandRuta = connection.CreateCommand())
        {
            commandRuta.CommandText = @"
                SELECT CAST(
                    SERVERPROPERTY('InstanceDefaultBackupPath')
                    AS nvarchar(4000)
                );";

            commandRuta.CommandTimeout = 30;

            var resultadoRuta =
                await commandRuta.ExecuteScalarAsync();

            carpetaBackup =
                resultadoRuta as string;
        }

        if (string.IsNullOrWhiteSpace(carpetaBackup))
        {
            throw new InvalidOperationException(
                "SQL Server no informó una carpeta predeterminada de respaldos.");
        }

        carpetaBackup = carpetaBackup.Trim();

        string marcaTiempo =
            DateTime.Now.ToString("yyyy-MM-dd_HHmmss");

        string nombreArchivo =
            $"{nombreBaseDatos}_{marcaTiempo}.bak";

        string rutaCompleta =
            Path.Combine(
                carpetaBackup,
                nombreArchivo);

        string nombreBaseSeguro =
            nombreBaseDatos.Replace("]", "]]");

        await using (var commandBackup = connection.CreateCommand())
        {
            commandBackup.CommandText = $@"
                BACKUP DATABASE [{nombreBaseSeguro}]
                TO DISK = @Ruta
                WITH
                    COPY_ONLY,
                    INIT,
                    CHECKSUM,
                    NAME = @NombreRespaldo;";

            commandBackup.Parameters.AddWithValue(
                "@Ruta",
                rutaCompleta);

            commandBackup.Parameters.AddWithValue(
                "@NombreRespaldo",
                $"Respaldo EscuelaInventario {DateTime.Now:dd/MM/yyyy HH:mm:ss}");

            commandBackup.CommandTimeout = 300;

            await commandBackup.ExecuteNonQueryAsync();
        }

        await using (var commandVerify = connection.CreateCommand())
        {
            commandVerify.CommandText = @"
                RESTORE VERIFYONLY
                FROM DISK = @Ruta
                WITH CHECKSUM;";

            commandVerify.Parameters.AddWithValue(
                "@Ruta",
                rutaCompleta);

            commandVerify.CommandTimeout = 300;

            await commandVerify.ExecuteNonQueryAsync();
        }

        return new ResultadoBackup
        {
            Correcto = true,
            NombreArchivo = nombreArchivo,
            RutaCompleta = rutaCompleta,
            FechaCreacion = DateTime.Now
        };
    }
}

public class ResultadoBackup
{
    public bool Correcto { get; set; }

    public string NombreArchivo { get; set; } =
        string.Empty;

    public string RutaCompleta { get; set; } =
        string.Empty;

    public DateTime FechaCreacion { get; set; }
}
