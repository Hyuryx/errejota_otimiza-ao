using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ErrejotaMatrixOptimizer
{
    public class UpdateInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("release_date")]
        public string ReleaseDate { get; set; } = "";

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = "";

        [JsonPropertyName("downloadUrl")]
        public string DownloadUrlCamel { set { if (string.IsNullOrEmpty(DownloadUrl)) DownloadUrl = value; } }

        [JsonPropertyName("changelog")]
        public string Changelog { get; set; } = "";

        [JsonPropertyName("mandatory")]
        public bool Mandatory { get; set; } = false;
    }

    public class GitHubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("published_at")]
        public string? PublishedAt { get; set; }

        [JsonPropertyName("assets")]
        public GitHubAssetDto[]? Assets { get; set; }
    }

    public class GitHubAssetDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }

    public class UpdateManager
    {
        // Obtém a versão normalizada do próprio executável compilado
        public static readonly Version CurrentVersion = GetNormalizedCurrentVersion();

        public static string CurrentVersionString => $"v{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";
        public static string CurrentVersionNumber => $"{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";

        // Endpoints oficiais
        public const string GitHubApiLatestReleaseUrl = "https://api.github.com/repos/Hyuryx/errejota_otimiza-ao/releases/latest";
        public const string DefaultUpdateUrl = "https://raw.githubusercontent.com/Hyuryx/errejota_otimiza-ao/main/version.json";

        public event Action<long, long, double>? OnDownloadProgress; // downloadedBytes, totalBytes, percent

        private static Version GetNormalizedCurrentVersion()
        {
            try
            {
                var asmVer = typeof(UpdateManager).Assembly.GetName().Version;
                if (asmVer != null)
                {
                    return new Version(asmVer.Major, Math.Max(0, asmVer.Minor), Math.Max(0, asmVer.Build));
                }
            }
            catch { }
            return new Version(1, 1, 0);
        }

        public static Version ParseVersionSafe(string? versionStr)
        {
            if (string.IsNullOrWhiteSpace(versionStr)) return new Version(1, 0, 0);
            string clean = versionStr.Trim().TrimStart('v', 'V').Trim();
            if (clean.Contains('-')) clean = clean.Split('-')[0];
            if (clean.Contains('+')) clean = clean.Split('+')[0];

            var parts = clean.Split('.');
            if (parts.Length == 1 && int.TryParse(parts[0], out int p1))
            {
                return new Version(p1, 0, 0);
            }
            if (parts.Length == 2 && int.TryParse(parts[0], out p1) && int.TryParse(parts[1], out int p2))
            {
                return new Version(p1, p2, 0);
            }
            if (Version.TryParse(clean, out var parsed))
            {
                return new Version(parsed.Major, Math.Max(0, parsed.Minor), Math.Max(0, parsed.Build));
            }
            return new Version(1, 0, 0);
        }

        public async Task<UpdateInfo?> CheckForUpdateAsync(string? customUrl = null)
        {
            // 1. PRIORIDADE MÁXIMA: Consulta direta na API Oficial do GitHub Releases (Zero Cache de CDN)
            if (string.IsNullOrWhiteSpace(customUrl))
            {
                try
                {
                    using var apiReq = new HttpRequestMessage(HttpMethod.Get, GitHubApiLatestReleaseUrl);
                    apiReq.Headers.Add("User-Agent", "ErreJota-Matrix-Optimizer-Pro");
                    apiReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
                    using var res = await client.SendAsync(apiReq);
                    if (res.IsSuccessStatusCode)
                    {
                        string apiJson = await res.Content.ReadAsStringAsync();
                        var release = JsonSerializer.Deserialize<GitHubReleaseDto>(apiJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (release != null && !string.IsNullOrWhiteSpace(release.TagName))
                        {
                            var remoteVer = ParseVersionSafe(release.TagName);
                            if (remoteVer > CurrentVersion)
                            {
                                string exeDownloadUrl = "";
                                if (release.Assets != null)
                                {
                                    foreach (var asset in release.Assets)
                                    {
                                        if (asset.Name != null && asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(asset.BrowserDownloadUrl))
                                        {
                                            exeDownloadUrl = asset.BrowserDownloadUrl;
                                            break;
                                        }
                                    }
                                }

                                if (string.IsNullOrEmpty(exeDownloadUrl))
                                {
                                    string tag = release.TagName.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? release.TagName : $"v{release.TagName}";
                                    exeDownloadUrl = $"https://github.com/Hyuryx/errejota_otimiza-ao/releases/download/{tag}/ErreJota_Matrix_Optimizer_Pro.exe";
                                }

                                string releaseDate = "";
                                if (!string.IsNullOrWhiteSpace(release.PublishedAt) && DateTime.TryParse(release.PublishedAt, out var dt))
                                {
                                    releaseDate = dt.ToString("dd/MM/yyyy");
                                }

                                return new UpdateInfo
                                {
                                    Version = $"{remoteVer.Major}.{remoteVer.Minor}.{remoteVer.Build}",
                                    ReleaseDate = releaseDate,
                                    DownloadUrl = exeDownloadUrl,
                                    Changelog = !string.IsNullOrWhiteSpace(release.Body) ? release.Body : (release.Name ?? "Atualização oficial ErreJota Matrix Optimizer Pro")
                                };
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback para o version.json abaixo
                }
            }

            // 2. FALLBACK SEGUNDO: Consulta version.json com bypass de cache HTTP
            try
            {
                string baseUrl = customUrl ?? DefaultUpdateUrl;
                // Previne cache do CDN do Fastly/GitHub adicionando timestamp único
                string url = baseUrl.Contains('?') ? $"{baseUrl}&_nocache={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}" : $"{baseUrl}?_nocache={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Add("User-Agent", "ErreJota-Matrix-Optimizer-Pro");
                req.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
                using var res = await client.SendAsync(req);
                if (res.IsSuccessStatusCode)
                {
                    string json = await res.Content.ReadAsStringAsync();
                    var info = JsonSerializer.Deserialize<UpdateInfo>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    if (info != null)
                    {
                        var remoteVer = ParseVersionSafe(info.Version);
                        if (string.IsNullOrWhiteSpace(info.DownloadUrl))
                        {
                            info.DownloadUrl = $"https://github.com/Hyuryx/errejota_otimiza-ao/releases/download/v{remoteVer.Major}.{remoteVer.Minor}.{remoteVer.Build}/ErreJota_Matrix_Optimizer_Pro.exe";
                        }

                        if (remoteVer > CurrentVersion)
                        {
                            // 1. Tenta a URL original configurada
                            if (await IsUrlReachableAsync(info.DownloadUrl))
                            {
                                info.Version = $"{remoteVer.Major}.{remoteVer.Minor}.{remoteVer.Build}";
                                return info;
                            }

                            // 2. Tenta variação com .0 na tag (ex: v1.2.1.0)
                            string altUrl = $"https://github.com/Hyuryx/errejota_otimiza-ao/releases/download/v{remoteVer.Major}.{remoteVer.Minor}.{remoteVer.Build}.0/ErreJota_Matrix_Optimizer_Pro.exe";
                            if (await IsUrlReachableAsync(altUrl))
                            {
                                info.DownloadUrl = altUrl;
                                info.Version = $"{remoteVer.Major}.{remoteVer.Minor}.{remoteVer.Build}";
                                return info;
                            }

                            // Se o executável ainda não foi publicado na Release no GitHub (link 404),
                            // não abre janela de erro de download para o usuário
                            return null;
                        }
                    }
                }
            }
            catch
            {
                // Sem conexão ou URL temporariamente inacessível
            }

            return null;
        }

        public static async Task<bool> IsUrlReachableAsync(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                client.DefaultRequestHeaders.Add("User-Agent", "ErreJota-Matrix-Optimizer-Pro");
                using var req = new HttpRequestMessage(HttpMethod.Head, url);
                using var res = await client.SendAsync(req);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
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
                    client.DefaultRequestHeaders.Add("User-Agent", "ErreJota-Matrix-Optimizer-Pro");

                    HttpResponseMessage? response = null;
                    try
                    {
                        response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    }
                    catch { }

                    // Se a URL principal falhar (ex: 404), tenta fallback com tag .0
                    if (response == null || !response.IsSuccessStatusCode)
                    {
                        response?.Dispose();
                        string altUrl = downloadUrl.Replace($"/v{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}/", $"/v{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}.0/");
                        if (altUrl != downloadUrl)
                        {
                            try
                            {
                                response = await client.GetAsync(altUrl, HttpCompletionOption.ResponseHeadersRead);
                            }
                            catch { }
                        }
                    }

                    if (response == null || !response.IsSuccessStatusCode)
                    {
                        response?.Dispose();
                        return false;
                    }

                    using (response)
                    {
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
                }

                // Cria o script autônomo de substituição e reinicialização com tolerância a múltiplos retries
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
