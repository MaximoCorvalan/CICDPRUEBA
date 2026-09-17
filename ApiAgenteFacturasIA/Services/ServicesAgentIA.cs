namespace ApiAgenteFacturasIA.Services
{
    using ApiAgenteFacturasIA.Converters;
    using ApiAgenteFacturasIA.Interfaces;
    using ApiAgenteFacturasIA.Models;
    using System.Diagnostics;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using Microsoft.Data.Sqlite;

    public class ServicesAgentIA : IAgentIAService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServicesAgentIA> _logger;

        public ServicesAgentIA(IConfiguration configuration, ILogger<ServicesAgentIA> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public ResultadoAnalisis parsearData(string json)
        {
            var inicio = json.IndexOf('{');
            var fin = json.LastIndexOf('}');

            if (inicio >= 0 && fin >= 0)
            {
                json = json.Substring(inicio, fin - inicio + 1);
            }

            var opciones = OpcionesJsonAgente();

            ResultadoAnalisis? resultado =
                JsonSerializer.Deserialize<ResultadoAnalisis>(json, opciones);

            if (resultado == null) { return new ResultadoAnalisis(); }
            return resultado;
        }

        internal static JsonSerializerOptions OpcionesJsonAgente()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                Converters = { new FlexibleDecimalConverterFactory() },
            };
        }

        private string ResolverRutaMemoriaDb(string agenteRoot)
        {
            var configurada = _configuration["AgenteFacturas:MemoriaDb"];
            if (!string.IsNullOrWhiteSpace(configurada))
            {
                var ruta = Path.GetFullPath(configurada);
                var dir = Path.GetDirectoryName(ruta);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                return ruta;
            }

            var fallback = Path.Combine(agenteRoot, "memoria", "memoria.db");
            Directory.CreateDirectory(Path.GetDirectoryName(fallback)!);
            return fallback;
        }

        public async Task<string> AgregarFeedback(FeedbackIA feedback)
        {
            try
            {
                if (!feedback.correcto)
                {
                    var estado = (feedback.estado_correcto ?? "").Trim().ToUpperInvariant();
                    if (estado is not ("LEGITIMA" or "SOSPECHOSA" or "FALSA"))
                    {
                        return "ERROR: si correcto=false, estado_correcto debe ser LEGITIMA, SOSPECHOSA o FALSA.";
                    }
                    feedback.estado_correcto = estado;
                }
                else if (string.IsNullOrWhiteSpace(feedback.estado_correcto))
                {
                    feedback.estado_correcto = feedback.estado_original;
                }

                var agenteRoot = ResolverRutaAgente();
                string rutaBD = ResolverRutaMemoriaDb(agenteRoot);

                await using var conexion = new SqliteConnection(
                    $"Data Source={rutaBD}"
                );

                await conexion.OpenAsync();

                string crearTablas = """
            CREATE TABLE IF NOT EXISTS feedback (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                fecha TEXT NOT NULL,
                documentos TEXT,
                tipo_documento TEXT,
                autenticidad_original REAL,
                estado_original TEXT,
                correcto INTEGER NOT NULL,
                comentario TEXT,
                estado_correcto TEXT,
                evidencia_json TEXT,
                origen TEXT,
                confirmado_por_supervisor INTEGER
            );

            CREATE TABLE IF NOT EXISTS correcciones (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                feedback_id INTEGER NOT NULL,
                campo TEXT NOT NULL,
                valor_sistema TEXT,
                valor_correcto TEXT,
                descripcion TEXT,

                FOREIGN KEY (feedback_id)
                    REFERENCES feedback(id)
                    ON DELETE CASCADE
            );
            """;

                await using (var comandoTablas = conexion.CreateCommand())
                {
                    comandoTablas.CommandText = crearTablas;
                    await comandoTablas.ExecuteNonQueryAsync();
                }

                // Migración suave de columnas nuevas
                await using (var pragma = conexion.CreateCommand())
                {
                    pragma.CommandText = "PRAGMA table_info(feedback)";
                    await using var reader = await pragma.ExecuteReaderAsync();
                    var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    while (await reader.ReadAsync())
                    {
                        columnas.Add(reader.GetString(1));
                    }
                    await reader.DisposeAsync();

                    if (!columnas.Contains("estado_correcto"))
                    {
                        await using var alter = conexion.CreateCommand();
                        alter.CommandText = "ALTER TABLE feedback ADD COLUMN estado_correcto TEXT";
                        await alter.ExecuteNonQueryAsync();
                    }
                    if (!columnas.Contains("evidencia_json"))
                    {
                        await using var alter = conexion.CreateCommand();
                        alter.CommandText = "ALTER TABLE feedback ADD COLUMN evidencia_json TEXT";
                        await alter.ExecuteNonQueryAsync();
                    }
                    if (!columnas.Contains("origen"))
                    {
                        await using var alter = conexion.CreateCommand();
                        alter.CommandText = "ALTER TABLE feedback ADD COLUMN origen TEXT";
                        await alter.ExecuteNonQueryAsync();
                    }
                    if (!columnas.Contains("confirmado_por_supervisor"))
                    {
                        await using var alter = conexion.CreateCommand();
                        alter.CommandText = "ALTER TABLE feedback ADD COLUMN confirmado_por_supervisor INTEGER";
                        await alter.ExecuteNonQueryAsync();
                    }
                }

                await using var transaccion = conexion.BeginTransaction();

                string documentosJson = JsonSerializer.Serialize(feedback.Documentos);
                string? evidenciaJson = null;
                if (feedback.evidencia_json.HasValue
                    && feedback.evidencia_json.Value.ValueKind is not JsonValueKind.Undefined
                        and not JsonValueKind.Null)
                {
                    evidenciaJson = feedback.evidencia_json.Value.GetRawText();
                }

                await using var comandoFeedback = conexion.CreateCommand();

                comandoFeedback.Transaction = transaccion;
                comandoFeedback.CommandText = """
            INSERT INTO feedback
            (
                fecha,
                documentos,
                tipo_documento,
                autenticidad_original,
                estado_original,
                correcto,
                comentario,
                estado_correcto,
                evidencia_json,
                origen,
                confirmado_por_supervisor
            )
            VALUES
            (
                @fecha,
                @documentos,
                @tipo_documento,
                @autenticidad_original,
                @estado_original,
                @correcto,
                @comentario,
                @estado_correcto,
                @evidencia_json,
                @origen,
                @confirmado_por_supervisor
            );

            SELECT last_insert_rowid();
            """;

                comandoFeedback.Parameters.AddWithValue(
                    "@fecha",
                    feedback.Fecha.ToString("O")
                );

                comandoFeedback.Parameters.AddWithValue(
                    "@documentos",
                    documentosJson
                );

                comandoFeedback.Parameters.AddWithValue(
                    "@tipo_documento",
                    (object?)feedback.tipo_documento ?? DBNull.Value
                );

                comandoFeedback.Parameters.AddWithValue(
                    "@autenticidad_original",
                    (object?)feedback.autenticidad_original ?? DBNull.Value
                );

                comandoFeedback.Parameters.AddWithValue(
                    "@estado_original",
                    (object?)feedback.estado_original ?? DBNull.Value
                );

                comandoFeedback.Parameters.AddWithValue(
                    "@correcto",
                    feedback.correcto ? 1 : 0
                );

                comandoFeedback.Parameters.AddWithValue(
                    "@comentario",
                    (object?)feedback.comentario ?? DBNull.Value
                );

                comandoFeedback.Parameters.AddWithValue(
                    "@estado_correcto",
                    (object?)feedback.estado_correcto ?? DBNull.Value
                );

                comandoFeedback.Parameters.AddWithValue(
                    "@evidencia_json",
                    (object?)evidenciaJson ?? DBNull.Value
                );
                comandoFeedback.Parameters.AddWithValue(
                    "@origen",
                    "ui"
                );
                comandoFeedback.Parameters.AddWithValue(
                    "@confirmado_por_supervisor",
                    feedback.correcto ? 1 : 0
                );

                long feedbackId = (long)(await comandoFeedback.ExecuteScalarAsync())!;

                foreach (var correccion in feedback.Correcciones)
                {
                    await using var comandoCorreccion = conexion.CreateCommand();

                    comandoCorreccion.Transaction = transaccion;

                    comandoCorreccion.CommandText = """
                INSERT INTO correcciones
                (
                    feedback_id,
                    campo,
                    valor_sistema,
                    valor_correcto,
                    descripcion
                )
                VALUES
                (
                    @feedback_id,
                    @campo,
                    @valor_sistema,
                    @valor_correcto,
                    @descripcion
                );
                """;

                    comandoCorreccion.Parameters.AddWithValue(
                        "@feedback_id",
                        feedbackId
                    );
                    comandoCorreccion.Parameters.AddWithValue(
                       "@descripcion", correccion.Descripcion ?? (object?)DBNull.Value
                   );

                    comandoCorreccion.Parameters.AddWithValue(
                        "@campo",
                        correccion.Campo
                    );

                    comandoCorreccion.Parameters.AddWithValue(
                        "@valor_sistema",
                        (object?)correccion.valor_sistema ?? DBNull.Value
                    );

                    comandoCorreccion.Parameters.AddWithValue(
                        "@valor_correcto",
                        (object?)correccion.valor_correcto ?? DBNull.Value
                    );

                    await comandoCorreccion.ExecuteNonQueryAsync();
                }

                await transaccion.CommitAsync();

                var politicaMsg = await ActualizarPoliticaLocalAsync(agenteRoot, feedbackId);
                return $"Feedback guardado correctamente. ID: {feedbackId}. {politicaMsg}";
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }

        private async Task<string> ActualizarPoliticaLocalAsync(string agenteRoot, long feedbackId)
        {
            try
            {
                var python = ResolverPython(agenteRoot);
                var proceso = new Process();
                proceso.StartInfo.FileName = python;
                proceso.StartInfo.Arguments =
                    "-c \"from revision_supervisor import aplicar_feedback_post_guardado; import json; print(json.dumps(aplicar_feedback_post_guardado("
                    + feedbackId
                    + "), ensure_ascii=False))\"";
                proceso.StartInfo.WorkingDirectory = agenteRoot;
                proceso.StartInfo.RedirectStandardOutput = true;
                proceso.StartInfo.RedirectStandardError = true;
                proceso.StartInfo.UseShellExecute = false;
                proceso.StartInfo.CreateNoWindow = true;
                proceso.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                proceso.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                proceso.Start();
                var salida = await proceso.StandardOutput.ReadToEndAsync();
                var error = await proceso.StandardError.ReadToEndAsync();
                await proceso.WaitForExitAsync();

                if (proceso.ExitCode != 0)
                {
                    _logger.LogWarning("No se pudo actualizar política local: {Error}", error);
                    return "Política local no actualizada (revisar logs).";
                }

                var derivado = false;
                try
                {
                    using var doc = JsonDocument.Parse(salida.Trim());
                    if (doc.RootElement.TryGetProperty("casilla", out var casilla)
                        && casilla.ValueKind == JsonValueKind.Object
                        && casilla.TryGetProperty("derivado", out var flag))
                    {
                        derivado = flag.ValueKind == JsonValueKind.True
                            || (flag.ValueKind == JsonValueKind.False ? false : flag.GetBoolean());
                        if (!derivado && casilla.TryGetProperty("ya_estaba", out var ya)
                            && ya.ValueKind == JsonValueKind.True)
                        {
                            derivado = true;
                        }
                    }
                }
                catch (JsonException)
                {
                    // La política puede haberse actualizado igual; el extra de casilla es informativo.
                }

                var msg = "Política local actualizada (rige en el próximo análisis). Pendiente reentrenar modelo GGUF.";
                if (derivado)
                    msg += " Derivado a casilla del supervisor.";
                return msg;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error actualizando política local");
                return "Política local no actualizada.";
            }
        }

        private string ResolverRutaAgente()
        {
            var configurada = _configuration["AgenteFacturas:Root"];
            var anclas = new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "agente contable"),
            };

            if (!string.IsNullOrWhiteSpace(configurada))
            {
                if (Path.IsPathRooted(configurada)
                    && Directory.Exists(configurada)
                    && File.Exists(Path.Combine(configurada, "main.py")))
                {
                    return Path.GetFullPath(configurada);
                }

                foreach (var ancla in anclas)
                {
                    var candidata = Path.GetFullPath(Path.Combine(ancla, configurada));
                    if (Directory.Exists(candidata) && File.Exists(Path.Combine(candidata, "main.py")))
                        return candidata;
                }
            }

            var nombres = new[] { "2026-08-21_1900", "AGENTEFACTURAS" };
            var relativos = new[]
            {
                ".",
                "..",
                Path.Combine("..", "..", "..", ".."),
            };

            foreach (var ancla in anclas)
            {
                foreach (var nombre in nombres)
                {
                    foreach (var relativo in relativos)
                    {
                        var candidato = Path.GetFullPath(Path.Combine(ancla, relativo, nombre));
                        if (Directory.Exists(candidato) && File.Exists(Path.Combine(candidato, "main.py")))
                            return candidato;
                    }
                }
            }

            throw new DirectoryNotFoundException(
                "No se encontro el agente (2026-08-21_1900). Configura AgenteFacturas:Root o copia Desktop\\agente contable\\2026-08-21_1900.");
        }

        private string ResolverPython(string agenteRoot)
        {
            var configurada = _configuration["AgenteFacturas:PythonExe"];
            if (!string.IsNullOrWhiteSpace(configurada) && File.Exists(configurada))
                return Path.GetFullPath(configurada);

            var local = Path.Combine(agenteRoot, "venv", "Scripts", "python.exe");
            if (File.Exists(local))
                return local;

            throw new FileNotFoundException("No se encontró el intérprete de Python del agente.", local);
        }

        private string ResolverScript(string agenteRoot)
        {
            var configurada = _configuration["AgenteFacturas:ScriptPath"];
            if (!string.IsNullOrWhiteSpace(configurada) && File.Exists(configurada))
                return Path.GetFullPath(configurada);

            var local = Path.Combine(agenteRoot, "main.py");
            if (File.Exists(local))
                return local;

            throw new FileNotFoundException("No se encontró main.py del agente.", local);
        }

        private string ResolverCarpetaFacturas(string agenteRoot)
        {
            var configurada = _configuration["AgenteFacturas:FacturasDir"]
                ?? _configuration["AgenteFacturas:DocumentosDir"];

            if (!string.IsNullOrWhiteSpace(configurada))
            {
                var ruta = Path.GetFullPath(configurada);
                Directory.CreateDirectory(ruta);
                return ruta;
            }

            var documentos = Path.Combine(agenteRoot, "documentos");
            Directory.CreateDirectory(documentos);
            return documentos;
        }

        public async Task<ResultadoAnalisis> AnalizarFactura(IFormFile archivo)
        {
            var agenteRoot = ResolverRutaAgente();
            var carpeta = ResolverCarpetaFacturas(agenteRoot);

            var nombreBase = Path.GetFileNameWithoutExtension(archivo.FileName);
            var extension = Path.GetExtension(archivo.FileName);
            var nombre = $"{nombreBase}{DateTime.Now:dd-MM-yyyy}{extension}";
            var ruta = Path.Combine(carpeta, nombre);

            using (var stream = new FileStream(ruta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            var python = ResolverPython(agenteRoot);
            var script = ResolverScript(agenteRoot);

            _logger.LogInformation("Ejecutando agente: {Python} {Script} --factura {Ruta}", python, script, ruta);

            var proceso = new Process();

            proceso.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            proceso.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            proceso.StartInfo.FileName = python;
            proceso.StartInfo.Arguments = $"\"{script}\" --factura \"{ruta}\"";
            proceso.StartInfo.WorkingDirectory = agenteRoot;
            proceso.StartInfo.RedirectStandardOutput = true;
            proceso.StartInfo.RedirectStandardError = true;
            proceso.StartInfo.UseShellExecute = false;
            proceso.StartInfo.CreateNoWindow = true;

            proceso.Start();

            var stdoutTask = proceso.StandardOutput.ReadToEndAsync();
            var stderrTask = proceso.StandardError.ReadToEndAsync();
            await proceso.WaitForExitAsync();
            string json = await stdoutTask;
            string error = await stderrTask;

            if (proceso.ExitCode != 0)
            {
                throw new Exception($"Error ejecutando Python: {error}");
            }

            ResultadoAnalisis result = parsearData(json);

            return result;
        }

        public async Task<ChatResponse> Chat(ChatRequest request)
        {
            var agenteRoot = ResolverRutaAgente();
            var python = ResolverPython(agenteRoot);
            var script = ResolverScript(agenteRoot);

            var opcionesOut = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            var jsonPayload = JsonSerializer.Serialize(request, opcionesOut);

            _logger.LogInformation("Ejecutando chat agente: {Python} {Script} --chat", python, script);

            var proceso = new Process();
            proceso.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            proceso.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            proceso.StartInfo.FileName = python;
            proceso.StartInfo.Arguments = $"\"{script}\" --chat";
            proceso.StartInfo.WorkingDirectory = agenteRoot;
            proceso.StartInfo.RedirectStandardInput = true;
            proceso.StartInfo.RedirectStandardOutput = true;
            proceso.StartInfo.RedirectStandardError = true;
            proceso.StartInfo.UseShellExecute = false;
            proceso.StartInfo.CreateNoWindow = true;

            proceso.Start();

            await proceso.StandardInput.WriteAsync(jsonPayload);
            proceso.StandardInput.Close();

            var stdoutTask = proceso.StandardOutput.ReadToEndAsync();
            var stderrTask = proceso.StandardError.ReadToEndAsync();
            await proceso.WaitForExitAsync();
            string json = await stdoutTask;
            string error = await stderrTask;

            if (proceso.ExitCode != 0)
            {
                throw new Exception($"Error ejecutando chat Python: {error}");
            }

            var inicio = json.IndexOf('{');
            var fin = json.LastIndexOf('}');
            if (inicio >= 0 && fin >= inicio)
            {
                json = json.Substring(inicio, fin - inicio + 1);
            }

            var opciones = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var respuesta = JsonSerializer.Deserialize<ChatResponse>(json, opciones);
            if (respuesta == null)
            {
                return new ChatResponse
                {
                    respuesta = "No se pudo interpretar la respuesta del agente de chat.",
                    fuera_de_ambito = false
                };
            }

            return respuesta;
        }

        public async Task ChatStream(
            ChatRequest request,
            Func<string, Task> writeLine,
            CancellationToken cancellationToken)
        {
            var agenteRoot = ResolverRutaAgente();
            var python = ResolverPython(agenteRoot);
            var script = ResolverScript(agenteRoot);

            var opcionesOut = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            var jsonPayload = JsonSerializer.Serialize(request, opcionesOut);

            _logger.LogInformation("Ejecutando chat stream: {Python} {Script} --chat", python, script);

            using var proceso = new Process();
            proceso.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            proceso.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            proceso.StartInfo.FileName = python;
            proceso.StartInfo.Arguments = $"\"{script}\" --chat";
            proceso.StartInfo.WorkingDirectory = agenteRoot;
            proceso.StartInfo.RedirectStandardInput = true;
            proceso.StartInfo.RedirectStandardOutput = true;
            proceso.StartInfo.RedirectStandardError = true;
            proceso.StartInfo.UseShellExecute = false;
            proceso.StartInfo.CreateNoWindow = true;

            proceso.Start();

            await using var killReg = cancellationToken.Register(() =>
            {
                try
                {
                    if (!proceso.HasExited)
                        proceso.Kill(entireProcessTree: true);
                }
                catch
                {
                    // proceso ya cerrado
                }
            });

            await proceso.StandardInput.WriteAsync(jsonPayload);
            proceso.StandardInput.Close();

            var writeLock = new SemaphoreSlim(1, 1);
            async Task Emitir(string linea)
            {
                await writeLock.WaitAsync(cancellationToken);
                try
                {
                    await writeLine(linea);
                }
                finally
                {
                    writeLock.Release();
                }
            }

            var errores = new System.Collections.Concurrent.ConcurrentBag<string>();
            var stderrTask = Task.Run(async () =>
            {
                while (true)
                {
                    var linea = await proceso.StandardError.ReadLineAsync();
                    if (linea == null)
                        break;
                    if (linea.StartsWith("AGENTE_UI ", StringComparison.Ordinal))
                    {
                        var json = linea.Substring("AGENTE_UI ".Length).Trim();
                        if (!string.IsNullOrWhiteSpace(json))
                            await Emitir(json);
                    }
                    else if (!string.IsNullOrWhiteSpace(linea))
                    {
                        errores.Add(linea);
                    }
                }
            }, cancellationToken);

            string stdout = await proceso.StandardOutput.ReadToEndAsync();
            await stderrTask;
            await proceso.WaitForExitAsync(cancellationToken);

            if (proceso.ExitCode != 0)
            {
                var error = string.Join("\n", errores);
                throw new Exception($"Error ejecutando chat Python: {error}");
            }

            var jsonFinal = stdout ?? "";
            var inicio = jsonFinal.IndexOf('{');
            var fin = jsonFinal.LastIndexOf('}');
            if (inicio >= 0 && fin >= inicio)
                jsonFinal = jsonFinal.Substring(inicio, fin - inicio + 1);

            try
            {
                if (JsonNode.Parse(jsonFinal) is JsonObject nodo)
                {
                    nodo["type"] = "done";
                    await Emitir(nodo.ToJsonString());
                    return;
                }
            }
            catch (JsonException)
            {
                // cae al fallback
            }

            await Emitir(JsonSerializer.Serialize(new
            {
                type = "done",
                respuesta = "No se pudo interpretar la respuesta del agente de chat.",
                fuera_de_ambito = false,
                herramientas_usadas = Array.Empty<string>(),
            }));
        }

        public string ResolverDocumento(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                throw new FileNotFoundException("Falta la ruta del documento.");

            var full = Path.GetFullPath(ruta.Trim().Trim('"'));
            if (!File.Exists(full))
                throw new FileNotFoundException("No se encontro el documento.", full);

            var agenteRoot = ResolverRutaAgente();
            var raices = new[]
            {
                Path.Combine(agenteRoot, "documentos"),
                Path.Combine(agenteRoot, "temp"),
                Path.Combine(agenteRoot, "memoria"),
                ResolverCarpetaFacturas(agenteRoot),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documentos"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents"),
            };

            foreach (var raiz in raices)
            {
                if (string.IsNullOrWhiteSpace(raiz)) continue;
                string fullRaiz;
                try { fullRaiz = Path.GetFullPath(raiz); }
                catch { continue; }
                var r = fullRaiz.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.Equals(full, r, StringComparison.OrdinalIgnoreCase)
                    || full.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return full;
                }
            }

            throw new UnauthorizedAccessException("El documento esta fuera del ambito del agente.");
        }
    }
}
