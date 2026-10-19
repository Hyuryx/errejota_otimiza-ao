using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ErrejotaMatrixOptimizer
{
    public class UpdateInfo
    {
        public string Version { get; set; } = "1.0.0";
        public string ReleaseDate { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string Changelog { get; set; } = "";
        public bool Mandatory { get; set; } = false;
    }

    public class UpdateManager
    {
        public static readonly Version CurrentVersion = typeof(UpdateManager).Assembly.GetName().Version ?? new Version(1, 0, 0);

        // URL padrão oficial no repositório GitHub do ErreJota
        public const string DefaultUpdateUrl = "https://raw.githubusercontent.com/Hyuryx/errejota_otimiza-ao/main/version.json";

        public event Action<long, long, double>? OnDownloadProgress; // downloadedBytes, totalBytes, percent

        public async Task<UpdateInfo?> CheckForUpdateAsync(string? customUrl = null)
        {
            string url = customUrl ?? DefaultUpdateUrl;
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(6);
                client.DefaultRequestHeaders.Add("User-Agent", "ErreJota-Matrix-Optimizer-Pro");

                string json = await client.GetStringAsync(url);
                var info = JsonSerializer.Deserialize<UpdateInfo>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (info != null && Version.TryParse(info.Version, out var remoteVer))
                {
                    if (remoteVer > CurrentVersion)
                    {
                        return info;
                    }
                }
            }
            catch
            {
                // Sem conexão ou URL ainda não publicada online
            }
            return null;
        }

        public async Task<bool> DownloadAndApplyUpdateAsync(string downloadUrl, string targetExePath)
        {
            string tempFile = targetExePath + ".new";
            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                    using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    long totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                    byte[] buffer = new byte[81920];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        double pct = totalBytes > 0 ? (double)totalRead / totalBytes * 100.0 : 0.0;
                        OnDownloadProgress?.Invoke(totalRead, totalBytes, pct);
                    }
                }

                // Cria o script autônomo de substituição e reinicialização
                string batchScript = Path.Combine(Path.GetTempPath(), "errejota_updater.cmd");
                string currentExe = targetExePath;
                string newExe = tempFile;

                string scriptContent = $@"@echo off
timeout /t 2 /nobreak > NUL
:retry
del ""{currentExe}"" 2>NUL
if exist ""{currentExe}"" (
    timeout /t 1 /nobreak > NUL
    goto retry
)
move /y ""{newExe}"" ""{currentExe}"" > NUL
start """" ""{currentExe}""
del ""%~f0"" & exit
";
                await File.WriteAllTextAsync(batchScript, scriptContent);

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchScript}\"",
                    CreateNoWindow = true,
                    UseShellExecute = true
                };
                Process.Start(psi);

                return true;
            }
            catch (Exception)
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
                return false;
            }
        }
    }
}
