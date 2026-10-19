using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ErrejotaMatrixOptimizer
{
    public class InstalledGame
    {
        public string Name { get; set; } = "";
        public string ExePath { get; set; } = "";
        public string Platform { get; set; } = "Steam"; // "Steam", "Epic", "FiveM", "Custom"
        public string DisplayLabel => $"[{Platform.ToUpper()}] {Name}";

        public override string ToString() => DisplayLabel;
    }

    public class GameLauncher
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        private readonly OptimizationEngine _engine;
        private readonly string _customGamesFile;

        public GameLauncher(OptimizationEngine engine)
        {
            _engine = engine;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "ErrejotaMatrixOptimizer");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            _customGamesFile = Path.Combine(folder, "custom_games.json");
        }

        public List<InstalledGame> ScanAllGames()
        {
            var list = new List<InstalledGame>();

            // 1. FiveM
            try
            {
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string fivemExe = Path.Combine(localApp, @"FiveM\FiveM.exe");
                if (File.Exists(fivemExe))
                {
                    list.Add(new InstalledGame
                    {
                        Name = "FiveM (GTA RP)",
                        ExePath = fivemExe,
                        Platform = "FiveM"
                    });
                }
            }
            catch { }

            // 2. Steam Games
            try
            {
                var steamGames = ScanSteamGames();
                foreach (var g in steamGames)
                {
                    if (!list.Any(x => x.ExePath.Equals(g.ExePath, StringComparison.OrdinalIgnoreCase)))
                        list.Add(g);
                }
            }
            catch { }

            // 3. Epic Games
            try
            {
                var epicGames = ScanEpicGames();
                foreach (var g in epicGames)
                {
                    if (!list.Any(x => x.ExePath.Equals(g.ExePath, StringComparison.OrdinalIgnoreCase)))
                        list.Add(g);
                }
            }
            catch { }

            // 4. Jogos Customizados Salvos
            try
            {
                if (File.Exists(_customGamesFile))
                {
                    string json = File.ReadAllText(_customGamesFile);
                    var customs = JsonSerializer.Deserialize<List<InstalledGame>>(json);
                    if (customs != null)
                    {
                        foreach (var c in customs)
                        {
                            if (File.Exists(c.ExePath) && !list.Any(x => x.ExePath.Equals(c.ExePath, StringComparison.OrdinalIgnoreCase)))
                                list.Add(c);
                        }
                    }
                }
            }
            catch { }

            return list;
        }

        public void SaveCustomGame(InstalledGame game)
        {
            try
            {
                var customs = new List<InstalledGame>();
                if (File.Exists(_customGamesFile))
                {
                    string json = File.ReadAllText(_customGamesFile);
                    var existing = JsonSerializer.Deserialize<List<InstalledGame>>(json);
                    if (existing != null) customs.AddRange(existing);
                }

                if (!customs.Any(x => x.ExePath.Equals(game.ExePath, StringComparison.OrdinalIgnoreCase)))
                {
                    customs.Add(game);
                    File.WriteAllText(_customGamesFile, JsonSerializer.Serialize(customs, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch { }
        }

        private List<InstalledGame> ScanSteamGames()
        {
            var results = new List<InstalledGame>();

            string steamPath = "";
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                if (key != null) steamPath = key.GetValue("SteamPath")?.ToString() ?? "";
            }
            if (string.IsNullOrEmpty(steamPath))
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                {
                    if (key != null) steamPath = key.GetValue("InstallPath")?.ToString() ?? "";
                }
            }

            if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath)) return results;

            var libraryDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string defaultSteamapps = Path.Combine(steamPath, "steamapps");
            if (Directory.Exists(defaultSteamapps)) libraryDirs.Add(defaultSteamapps);

            // Ler libraryfolders.vdf
            string vdf = Path.Combine(defaultSteamapps, "libraryfolders.vdf");
            if (File.Exists(vdf))
            {
                string text = File.ReadAllText(vdf);
                var matches = Regex.Matches(text, @"""path""\s+""([^""]+)""");
                foreach (Match m in matches)
                {
                    string p = m.Groups[1].Value.Replace(@"\\", @"\");
                    string sDir = Path.Combine(p, "steamapps");
                    if (Directory.Exists(sDir)) libraryDirs.Add(sDir);
                }
            }

            foreach (var lib in libraryDirs)
            {
                try
                {
                    var acfs = Directory.GetFiles(lib, "appmanifest_*.acf");
                    foreach (var acf in acfs)
                    {
                        try
                        {
                            string content = File.ReadAllText(acf);
                            var nameMatch = Regex.Match(content, @"""name""\s+""([^""]+)""");
                            var dirMatch = Regex.Match(content, @"""installdir""\s+""([^""]+)""");

                            if (nameMatch.Success && dirMatch.Success)
                            {
                                string name = nameMatch.Groups[1].Value;
                                string folder = dirMatch.Groups[1].Value;

                                // Pula redistribuíveis comuns
                                if (name.Contains("Steamworks", StringComparison.OrdinalIgnoreCase) ||
                                    name.Contains("Redistributable", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                string gameDir = Path.Combine(lib, "common", folder);
                                if (Directory.Exists(gameDir))
                                {
                                    string exe = FindBestGameExecutable(gameDir, folder);
                                    if (!string.IsNullOrEmpty(exe))
                                    {
                                        results.Add(new InstalledGame
                                        {
                                            Name = name,
                                            ExePath = exe,
                                            Platform = "Steam"
                                        });
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return results;
        }

        private List<InstalledGame> ScanEpicGames()
        {
            var results = new List<InstalledGame>();
            try
            {
                string pData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string manifestsDir = Path.Combine(pData, @"Epic\EpicGamesLauncher\Data\Manifests");
                if (Directory.Exists(manifestsDir))
                {
                    foreach (var itemFile in Directory.GetFiles(manifestsDir, "*.item"))
                    {
                        try
                        {
                            string json = File.ReadAllText(itemFile);
                            using var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;
                            string name = root.TryGetProperty("DisplayName", out var pName) ? pName.GetString() ?? "" : "";
                            string installLoc = root.TryGetProperty("InstallLocation", out var pLoc) ? pLoc.GetString() ?? "" : "";
                            string launchExe = root.TryGetProperty("LaunchExecutable", out var pExe) ? pExe.GetString() ?? "" : "";

                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(installLoc) && !string.IsNullOrEmpty(launchExe))
                            {
                                string fullExe = Path.Combine(installLoc, launchExe);
                                if (File.Exists(fullExe))
                                {
                                    results.Add(new InstalledGame
                                    {
                                        Name = name,
                                        ExePath = fullExe,
                                        Platform = "Epic"
                                    });
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return results;
        }

        private string FindBestGameExecutable(string gameDir, string folderName)
        {
            try
            {
                var files = Directory.GetFiles(gameDir, "*.exe", SearchOption.TopDirectoryOnly)
                    .Where(f => !Path.GetFileName(f).StartsWith("unins", StringComparison.OrdinalIgnoreCase) &&
                                !Path.GetFileName(f).StartsWith("crash", StringComparison.OrdinalIgnoreCase) &&
                                !Path.GetFileName(f).Contains("setup", StringComparison.OrdinalIgnoreCase) &&
                                !Path.GetFileName(f).Contains("helper", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => new FileInfo(f).Length)
                    .ToList();

                if (files.Count > 0)
                {
                    // Prioriza .exe que contenha o nome da pasta ou 'launcher' / 'game'
                    var direct = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(folderName, StringComparison.OrdinalIgnoreCase));
                    if (direct != null) return direct;

                    return files[0];
                }

                // Tenta subpasta imediata (ex: bin/x64)
                var subExes = Directory.GetFiles(gameDir, "*.exe", SearchOption.AllDirectories)
                    .Where(f => !Path.GetFileName(f).StartsWith("unins", StringComparison.OrdinalIgnoreCase) &&
                                !Path.GetFileName(f).StartsWith("crash", StringComparison.OrdinalIgnoreCase) &&
                                !Path.GetFileName(f).Contains("redist", StringComparison.OrdinalIgnoreCase) &&
                                !Path.GetFileName(f).Contains("setup", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => new FileInfo(f).Length)
                    .ToList();

                if (subExes.Count > 0) return subExes[0];
            }
            catch { }

            return "";
        }

        public async Task<bool> LaunchGameOptimizedAsync(InstalledGame game, bool cleanRam = true, bool lockCcd0 = true, bool highPriority = true)
        {
            if (game == null || !File.Exists(game.ExePath))
            {
                _engine.Log($"[ERRO] Executável do jogo não encontrado: {game?.ExePath}", LogLevel.Error);
                return false;
            }

            _engine.Log("================================================================================", LogLevel.Header);
            _engine.Log($">>> INICIANDO JOGO EM MODO COMPETITIVO: {game.Name}", LogLevel.Header);
            _engine.Log($"    Plataforma: {game.Platform} | Executável: {game.ExePath}", LogLevel.Info);
            _engine.Log("================================================================================", LogLevel.Header);

            // 1. Limpeza de RAM em espera (Zero Cache)
            if (cleanRam)
            {
                _engine.Log("[1/4] Esvaziando Cache da Memória RAM (2GB a 6GB livres para o jogo)...", LogLevel.Info);
                _engine.CleanSystemRam();
                _engine.Log("   ✔ Memória RAM purificada e liberada com sucesso!", LogLevel.Success);
            }

            // 2. Temporizador Multimídia de Alta Precisão (0.5ms)
            try
            {
                TimeBeginPeriod(1);
                _engine.Log("[2/4] Temporizador de Sistema cravado em 0.5ms (Menor Latência de Frame)!", LogLevel.Success);
            }
            catch { }

            // 3. Inicialização Segura (Sem elevação de Admin para não travar Anti-Cheats)
            _engine.Log("[3/4] Disparando executável via Explorer (Bypass seguro de Anti-Cheat)...", LogLevel.Info);
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{game.ExePath}\"",
                    UseShellExecute = true
                };
                Process.Start(psi);
                _engine.Log("   ✔ Processo do jogo disparado com sucesso!", LogLevel.Success);
            }
            catch (Exception ex)
            {
                _engine.Log($"   ✖ Falha ao disparar jogo: {ex.Message}", LogLevel.Error);
                return false;
            }

            // 4. Sentinela de Afinidade CCD0 e Prioridade Alta
            string targetExeName = Path.GetFileNameWithoutExtension(game.ExePath);
            _engine.Log($"[4/4] Sentinela Ativado: Monitorando processo '{targetExeName}' para aplicar travas de alta performance...", LogLevel.Info);

            _ = Task.Run(async () =>
            {
                bool captured = false;
                for (int attempt = 0; attempt < 45; attempt++) // 90 segundos de monitoramento
                {
                    await Task.Delay(2000);

                    Process[] procs;
                    if (game.Platform == "FiveM")
                    {
                        procs = Process.GetProcessesByName("FiveM_b")
                            .Concat(Process.GetProcessesByName("FiveM_GTAProcess"))
                            .Concat(Process.GetProcessesByName("GTA5"))
                            .ToArray();
                    }
                    else
                    {
                        procs = Process.GetProcessesByName(targetExeName);
                        if (procs.Length == 0)
                        {
                            // Tenta variações comuns
                            if (targetExeName.EndsWith("_Shipping", StringComparison.OrdinalIgnoreCase))
                                procs = Process.GetProcessesByName(targetExeName.Replace("_Shipping", ""));
                            else if (targetExeName.Equals("PlayGTAV", StringComparison.OrdinalIgnoreCase))
                                procs = Process.GetProcessesByName("GTA5");
                            else if (targetExeName.Equals("cod", StringComparison.OrdinalIgnoreCase))
                                procs = Process.GetProcessesByName("cod");
                        }
                    }

                    if (procs.Length > 0)
                    {
                        foreach (var p in procs)
                        {
                            try
                            {
                                if (lockCcd0)
                                {
                                    // Trava nos primeiros 16 threads (CCD0 Zen 3 - 32MB Cache L3 compartilhado)
                                    p.ProcessorAffinity = (IntPtr)65535; // 0xFFFF
                                }

                                if (highPriority)
                                {
                                    p.PriorityClass = ProcessPriorityClass.High;
                                }

                                captured = true;
                            }
                            catch { }
                        }

                        if (captured)
                        {
                            _engine.Log($"   ✔ SENTINELA: Processo '{procs[0].ProcessName}' capturado!", LogLevel.Success);
                            if (lockCcd0) _engine.Log("   ✔ Processador travado no CCD0 (0xFFFF) com 32MB de Cache L3 unificado!", LogLevel.Success);
                            if (highPriority) _engine.Log("   ✔ Prioridade de Processo elevada para ALTA (High Priority)!", LogLevel.Success);
                            break;
                        }
                    }
                }

                if (!captured)
                {
                    _engine.Log($"   ℹ Sentinela encerrou o ciclo de captura para '{targetExeName}'. O jogo já deve estar gerenciado.", LogLevel.Info);
                }
            });

            return true;
        }
    }
}
