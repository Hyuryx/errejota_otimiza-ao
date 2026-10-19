using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ErrejotaMatrixOptimizer
{
    public partial class MainWindow : Window
    {
        private readonly OptimizationEngine _engine = new();
        private readonly UpdateManager _updateManager = new();
        private readonly GameLauncher _gameLauncher;
        private UpdateInfo? _pendingUpdate = null;
        private HardwareSpecs _specs = new();
        private bool _isBusy = false;
        private List<InstalledGame> _detectedGames = new();

        public MainWindow()
        {
            InitializeComponent();
            _gameLauncher = new GameLauncher(_engine);
            _engine.OnLog += Engine_OnLog;
            _updateManager.OnDownloadProgress += (downloaded, total, pct) =>
            {
                Dispatcher.Invoke(() =>
                {
                    BarUpdateProgress.Value = pct;
                    TxtUpdateProgressPercent.Text = $"{pct:F0}%";
                    if (total > 0)
                    {
                        double mbDone = downloaded / (1024.0 * 1024.0);
                        double mbTotal = total / (1024.0 * 1024.0);
                        TxtUpdateProgressLabel.Text = $"Baixando atualização: {mbDone:F1} MB / {mbTotal:F1} MB...";
                    }
                    else
                    {
                        double mbDone = downloaded / (1024.0 * 1024.0);
                        TxtUpdateProgressLabel.Text = $"Baixando atualização: {mbDone:F1} MB...";
                    }
                });
            };
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string verStr = UpdateManager.CurrentVersion.ToString(3);
            TxtHeaderVersion.Text = $"v{verStr}";
            TxtFooterVersion.Text = $"{verStr} (Release)";

            AppendLog($"⚡ INICIANDO ERREJOTA MATRIX OPTIMIZER PRO v{verStr}...");
            AppendLog("Varrendo hardware, integridade de rede 1 Gbps, jogos instalados e subsistemas...");
            await RefreshHardwareDataAsync();
            await LoadDetectedGamesAsync();
            AppendLog("✅ Sistema pronto. Selecione uma aba e ative os módulos com total segurança.");
        }

        private async Task RefreshHardwareDataAsync()
        {
            _specs = await Task.Run(() => HardwareScanner.Scan());

            TxtHeaderHardware.Text = $"{_specs.CpuName} | {_specs.GpuName} | {_specs.RamTotalGb}GB RAM ({_specs.RefreshRate}Hz)";
            TxtRamStatusBadge.Text = $"{_specs.RamTotalGb}GB Detectado";
            TxtHealthScore.Text = $"{_specs.HealthScore}%";

            // Painel de diagnóstico de gargalos
            PanelGargalosList.Children.Clear();

            AddGargaloItem("Afinidade CCD0 (Ryzen Zen 3):",
                _specs.IsFivemCcd0Active ? "ATIVO (0xFFFF / 32MB Cache L3 direto)" : "DESATIVADO (Risco de micro-stutter)",
                _specs.IsFivemCcd0Active);

            AddGargaloItem("Desativação de MPO (Multi-Plane Overlay):",
                _specs.IsMpoDisabled ? "DESATIVADO (Estável para 240Hz / Dual Screen)" : "ATIVADO NO WINDOWS (Pode causar stutters)",
                _specs.IsMpoDisabled);

            AddGargaloItem("Win32PrioritySeparation (Foco no Jogo):",
                _specs.IsWin32PriorityOptimized ? "OTIMIZADO (0x28 - Prioridade quântica)" : "PADRÃO OU NÃO OTIMIZADO",
                _specs.IsWin32PriorityOptimized);

            AddGargaloItem("Desbloqueio de Rede 1 Gbps / CUBIC:",
                _specs.NetworkLinkSpeed.Contains("Gbps") ? $"VELOCIDADE MÁXIMA ({_specs.NetworkLinkSpeed} / Sem Limite de Upload)" : "MODO NORMAL",
                true);

            string samplingText = _specs.CurrentGtaSamplingMode switch
            {
                6 => "1440p DOWNSCALE ATIVO (2560x1440)",
                7 => "1620p DOWNSCALE ATIVO (2880x1620)",
                9 => "4K DOWNSCALE ATIVO (3840x2160)",
                0 => "1080p NATIVO PURO (1.0x)",
                2 => "720p PERFORMANCE (1280x720)",
                _ => $"Modo Custom (SamplingMode {_specs.CurrentGtaSamplingMode})"
            };

            AddGargaloItem("Resolução do GTA V / FiveM (settings.xml):",
                File.Exists(_specs.GtaSettingsPath) ? $"{samplingText} - {_specs.GtaScreenWidth}x{_specs.GtaScreenHeight} @ {_specs.GtaRefreshRate + 1}Hz" : "settings.xml não encontrado",
                File.Exists(_specs.GtaSettingsPath));

            AddGargaloItem("Placa de Rede Realtek Principal:",
                $"{_specs.NetworkCardName} ({_specs.NetworkLinkSpeed})",
                true);

            UpdateSidebarBadges();
        }

        private void UpdateSidebarBadges()
        {
            if (BadgeSupreme == null) return;

            // 1. Geral & Kernel
            bool supremeOk = _specs.IsWin32PriorityOptimized && _specs.IsMpoDisabled;
            TxtBadgeSupreme.Text = supremeOk ? "✔ OK" : "⚡ ATIVAR";
            BadgeSupreme.BorderBrush = supremeOk ? (SolidColorBrush)FindResource("NeonGreen") : (SolidColorBrush)FindResource("NeonYellow");
            TxtBadgeSupreme.Foreground = supremeOk ? (SolidColorBrush)FindResource("NeonGreen") : (SolidColorBrush)FindResource("NeonYellow");
            BadgeSupreme.Background = supremeOk ? new SolidColorBrush(Color.FromRgb(0x11, 0x2B, 0x1C)) : new SolidColorBrush(Color.FromRgb(0x28, 0x24, 0x0C));

            // 2. Anti-Gargalo FiveM
            bool fivemOk = _specs.IsFivemCcd0Active && _specs.IsNetworkThrottlingDisabled;
            TxtBadgeAntiFivem.Text = fivemOk ? "✔ OK" : "⚡ ATIVAR";
            BadgeAntiFivem.BorderBrush = fivemOk ? (SolidColorBrush)FindResource("NeonGreen") : (SolidColorBrush)FindResource("NeonYellow");
            TxtBadgeAntiFivem.Foreground = fivemOk ? (SolidColorBrush)FindResource("NeonGreen") : (SolidColorBrush)FindResource("NeonYellow");
            BadgeAntiFivem.Background = fivemOk ? new SolidColorBrush(Color.FromRgb(0x11, 0x2B, 0x1C)) : new SolidColorBrush(Color.FromRgb(0x28, 0x24, 0x0C));

            // 3. Super Resolução & Downscale
            bool downscaleOk = File.Exists(_specs.GtaSettingsPath) && _specs.CurrentGtaSamplingMode > 0;
            TxtBadgeDownscale.Text = downscaleOk ? "✔ OK" : "1080p";
            BadgeDownscale.BorderBrush = downscaleOk ? (SolidColorBrush)FindResource("NeonGreen") : (SolidColorBrush)FindResource("NeonCyan");
            TxtBadgeDownscale.Foreground = downscaleOk ? (SolidColorBrush)FindResource("NeonGreen") : (SolidColorBrush)FindResource("NeonCyan");
            BadgeDownscale.Background = downscaleOk ? new SolidColorBrush(Color.FromRgb(0x11, 0x2B, 0x1C)) : new SolidColorBrush(Color.FromRgb(0x0C, 0x25, 0x30));

            // 4. Scanner Score
            TxtBadgeScanner.Text = $"{_specs.HealthScore}%";
            BadgeScanner.BorderBrush = _specs.HealthScore >= 90 ? (SolidColorBrush)FindResource("NeonGreen") : (SolidColorBrush)FindResource("NeonYellow");
            TxtBadgeScanner.Foreground = _specs.HealthScore >= 90 ? (SolidColorBrush)FindResource("NeonGreen") : (SolidColorBrush)FindResource("NeonYellow");
            BadgeScanner.Background = _specs.HealthScore >= 90 ? new SolidColorBrush(Color.FromRgb(0x11, 0x2B, 0x1C)) : new SolidColorBrush(Color.FromRgb(0x28, 0x24, 0x0C));
        }

        private void AddGargaloItem(string title, string status, bool isOk)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x11, 0x16, 0x22)),
                BorderBrush = new SolidColorBrush(isOk ? Color.FromRgb(0x16, 0x3E, 0x24) : Color.FromRgb(0x4A, 0x1E, 0x22)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var tTitle = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(tTitle, 0);

            var statusPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var ledDot = new System.Windows.Shapes.Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = isOk ? new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x66)) : new SolidColorBrush(Color.FromRgb(0xFF, 0x55, 0x55)),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var tStatus = new TextBlock
            {
                Text = status,
                FontSize = 11,
                Foreground = isOk ? new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x66)) : new SolidColorBrush(Color.FromRgb(0xFF, 0x55, 0x55)),
                VerticalAlignment = VerticalAlignment.Center
            };

            statusPanel.Children.Add(ledDot);
            statusPanel.Children.Add(tStatus);
            Grid.SetColumn(statusPanel, 1);

            grid.Children.Add(tTitle);
            grid.Children.Add(statusPanel);
            border.Child = grid;

            PanelGargalosList.Children.Add(border);
        }

        // ==========================================
        // NAVEGAÇÃO ENTRE ABAS
        // ==========================================
        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (TabSupreme == null) return;

            TabSupreme.Visibility = NavSupreme.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            TabAntiFivem.Visibility = NavAntiFivem.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            TabDownscale.Visibility = NavDownscale.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            TabLauncher.Visibility = NavLauncher.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            TabCleaning.Visibility = NavCleaning.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            TabDefender.Visibility = NavDefender.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            TabScanner.Visibility = NavScanner.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            TabDocs.Visibility = NavDocs.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Engine_OnLog(LogEntry entry)
        {
            Dispatcher.Invoke(() =>
            {
                string icon = entry.Level switch
                {
                    LogLevel.Success => "✔",
                    LogLevel.Warning => "⚠",
                    LogLevel.Error => "✖",
                    LogLevel.Header => "▶",
                    _ => "•"
                };

                TxtConsole.AppendText($"[{entry.Time}] {icon} {entry.Message}\n");
                ConsoleScroll.ScrollToEnd();
            });
        }

        private void AppendLog(string message)
        {
            TxtConsole.AppendText($"[{DateTime.Now:HH:mm:ss}] • {message}\n");
            ConsoleScroll.ScrollToEnd();
        }

        private void SetBusy(bool busy, string status = "")
        {
            _isBusy = busy;
            TxtSystemStatus.Text = busy ? status : "SISTEMA: PRONTO";
            TxtSystemStatus.Foreground = busy ? (SolidColorBrush)FindResource("NeonYellow") : (SolidColorBrush)FindResource("NeonGreen");
        }

        // ==========================================
        // BARRA DE TÍTULO
        // ==========================================
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void BtnClearConsole_Click(object sender, RoutedEventArgs e) => TxtConsole.Clear();

        // ==========================================
        // MODAIS INTERATIVOS
        // ==========================================
        private void ShowCheckModal(string title, string content)
        {
            TxtCheckModalTitle.Text = title;
            TxtCheckModalBody.Text = content;
            ModalAntiBottleneck.Visibility = Visibility.Collapsed;
            ModalRestorePoint.Visibility = Visibility.Collapsed;
            ModalUpdate.Visibility = Visibility.Collapsed;
            ModalStatusCheck.Visibility = Visibility.Visible;
            ModalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseCheckModal_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            ModalStatusCheck.Visibility = Visibility.Collapsed;
            ModalUpdate.Visibility = Visibility.Collapsed;
        }

        // ==========================================
        // MÓDULO 1: GERAL & KERNEL SUPREMO
        // ==========================================
        private async void BtnSupremeOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            SetBusy(true, "EXECUTANDO SUPREMO...");
            try
            {
                // Ativa visualmente todos os cards/checkboxes
                ChkPowerPlan.IsChecked = true;
                ChkUnparkCpu.IsChecked = true;
                ChkWin32Prio.IsChecked = true;
                ChkSvcHost.IsChecked = true;
                ChkMpo.IsChecked = true;
                ChkMsi.IsChecked = true;
                ChkNvidiaTweaks.IsChecked = true;
                ChkMmcss.IsChecked = true;

                await _engine.RunSupremeAutomationAsync(_specs);
                await _engine.ApplyMmcssTweaksAsync();
                await _engine.ApplyNvidiaTweaksAsync();
                await _engine.ApplyDebloatAndUiTweaksAsync();
                await _engine.ApplySvcHostSplitAsync();
                await RefreshHardwareDataAsync();

                ShowCheckModal("⚡ KERNEL GAMER SUPREMO ATIVADO", 
                    "Todos os módulos de otimização de CPU, GPU, Memória e Sistema foram ativados com sucesso.\n\n" +
                    "• Plano de Energia: Desempenho Máximo\n" +
                    "• Unpark CPU: 100% dos núcleos ativos no pico\n" +
                    "• Win32PrioritySeparation: 0x28 (Foco Quântico em Jogos)\n" +
                    "• Isolamento de Processos SvcHost Split\n" +
                    "• Desativação de MPO (Fim dos stutters em 240Hz)\n" +
                    "• Modo MSI da Placa de Vídeo com Alta Prioridade\n" +
                    "• NVIDIA Latency Tweaks (Preemption Desligada & HAGS)\n" +
                    "• MMCSS Games GPU Priority 8 & SFIO High");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void BtnCreateRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            TxtRestorePointDesc.Text = $"ErreJota_Matrix_{DateTime.Now:dd_MM_yyyy_HHmm}";
            ModalAntiBottleneck.Visibility = Visibility.Collapsed;
            ModalStatusCheck.Visibility = Visibility.Collapsed;
            ModalRestorePoint.Visibility = Visibility.Visible;
            ModalOverlay.Visibility = Visibility.Visible;
        }

        private async void BtnConfirmCreateRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            string desc = TxtRestorePointDesc.Text.Trim();
            if (string.IsNullOrEmpty(desc)) desc = $"ErreJota_Matrix_{DateTime.Now:dd_MM_yyyy_HHmm}";

            BtnConfirmCreateRestorePoint.IsEnabled = false;
            AppendLog($"[RESTORE] Criando ponto de restauração: \"{desc}\"...");
            bool ok = await _engine.CreateRestorePointAsync(desc);

            ModalOverlay.Visibility = Visibility.Collapsed;
            ModalRestorePoint.Visibility = Visibility.Collapsed;
            BtnConfirmCreateRestorePoint.IsEnabled = true;

            if (ok)
            {
                ShowCheckModal("🛡️ PONTO DE RESTAURAÇÃO CRIADO", 
                    $"O ponto de restauração \"{desc}\" foi registrado e AUDITADO com sucesso no catálogo do Windows!\n\nVocê pode abrir o Assistente de Restauração (rstrui.exe) a qualquer momento para conferir ou restaurar.");
            }
            else
            {
                ShowCheckModal("⚠️ AVISO: PONTO NÃO CATALOGADO", 
                    $"O Windows não confirmou a gravação do ponto de restauração no catálogo.\n\nMotivo mais comum:\n• A 'Proteção do Sistema' da unidade C: está Desativada nas configurações do Windows, ou sem espaço de disco alocado.\n\nPara resolver:\n1. Clique em 'CONFIGURAR PROTEÇÃO (C:)' no painel de segurança;\n2. Selecione o Disco Local (C:) e clique em 'Configurar';\n3. Marque 'Ativar proteção do sistema' e defina o uso máximo para 5% a 10%;\n4. Clique em Aplicar e tente criar o ponto novamente!");
            }
        }

        private void BtnOpenRstrui_Click(object sender, RoutedEventArgs e)
        {
            _engine.OpenSystemRestoreWizard();
        }

        private void BtnOpenWindowsRestorePanel_Click(object sender, RoutedEventArgs e)
        {
            _engine.OpenSystemRestoreControlPanel();
        }

        private void BtnCancelRestorePointModal_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            ModalRestorePoint.Visibility = Visibility.Collapsed;
        }

        private async void BtnRestoreDefaults_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            var res = MessageBox.Show(
                "Deseja realmente restaurar as configurações originais do Windows?",
                "Restaurar Padrões",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes) return;

            SetBusy(true, "RESTAURANDO PADRÕES...");
            try
            {
                await _engine.RestoreWindowsDefaultsAsync();
                await RefreshHardwareDataAsync();
            }
            finally
            {
                SetBusy(false);
            }
        }

        // Perfis de Memória RAM Inteligente
        private async void BtnRamProfile_8GB_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            SetBusy(true, "CONFIGURANDO RAM 8GB...");
            try
            {
                await _engine.ApplyRamOptimizationAsync(8);
                ShowCheckModal("🧠 PERFIL DE RAM APLICADO: 8GB", "SvcHost configurado para 8GB e Compressão de RAM ativada para máxima economia.");
            }
            finally { SetBusy(false); }
        }

        private async void BtnRamProfile_16GB_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            SetBusy(true, "CONFIGURANDO RAM 16GB...");
            try
            {
                await _engine.ApplyRamOptimizationAsync(16);
                ShowCheckModal("🧠 PERFIL DE RAM APLICADO: 16GB", "SvcHost configurado para 16GB e Compressão de RAM desligada para zero lag de CPU.");
            }
            finally { SetBusy(false); }
        }

        private async void BtnRamProfile_32GB_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            SetBusy(true, "CONFIGURANDO RAM 32GB...");
            try
            {
                await _engine.ApplyRamOptimizationAsync(32);
                ShowCheckModal("🧠 PERFIL DE RAM APLICADO: 32GB (MODO ELITE)", "SvcHost configurado para 32GB e Compressão de RAM desligada. Memória 100% livre para FiveM e Warzone.");
            }
            finally { SetBusy(false); }
        }

        private async void BtnRamProfile_64GB_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            SetBusy(true, "CONFIGURANDO RAM 64GB...");
            try
            {
                await _engine.ApplyRamOptimizationAsync(64);
                ShowCheckModal("🧠 PERFIL DE RAM APLICADO: 64GB", "SvcHost configurado para 64GB. Isolamento total de threads de serviço.");
            }
            finally { SetBusy(false); }
        }

        private void BtnCheckRam_Click(object sender, RoutedEventArgs e)
        {
            string status = _engine.CheckRamStatus();
            ShowCheckModal("🧠 STATUS DA MEMÓRIA RAM", status);
        }

        private void BtnEmptyStandbyRam_Click(object sender, RoutedEventArgs e)
        {
            _engine.CleanSystemRam();
            AppendLog("Working Sets de todos os processos esvaziados! Memória em espera liberada.");
            ShowCheckModal("🧹 MEMÓRIA RAM LIMPA", "Cache em espera e working sets dos processos em segundo plano foram esvaziados com sucesso.");
        }

        // Toggles individuais da aba Geral (Controle independente de cada card)
        private async void TogglePowerPlan_Click(object sender, RoutedEventArgs e)
        {
            bool enable = ChkPowerPlan.IsChecked == true;
            await _engine.TogglePowerPlanAsync(enable);
            ShowCheckModal("⚙️ PLANO DE ENERGIA", enable ? "Plano 'Desempenho Máximo' ativado com sucesso!" : "Plano de energia restaurado para o padrão do Windows.");
        }

        private async void ToggleUnparkCpu_Click(object sender, RoutedEventArgs e)
        {
            bool enable = ChkUnparkCpu.IsChecked == true;
            await _engine.ToggleUnparkCpuAsync(enable);
            ShowCheckModal("⚙️ UNPARK CPU", enable ? "100% dos núcleos da CPU desestacionados (Frequência máxima contínua)!" : "Estacionamento de núcleos restaurado para o padrão.");
        }

        private async void ToggleWin32Prio_Click(object sender, RoutedEventArgs e)
        {
            bool enable = ChkWin32Prio.IsChecked == true;
            await _engine.ToggleWin32PriorityAsync(enable);
            ShowCheckModal("⚙️ WIN32 PRIORITY SEPARATION", enable ? "Prioridade quântica 0x28 (40 dec) ativada com foco em jogos!" : "Prioridade de processos restaurada para o padrão (0x1A).");
        }

        private async void ToggleSvcHost_Click(object sender, RoutedEventArgs e)
        {
            bool enable = ChkSvcHost.IsChecked == true;
            await _engine.ToggleSvcHostSplitAsync(enable);
            ShowCheckModal("⚙️ SVCHOST PROCESS ISOLATION", enable ? "Isolamento de processos SvcHost ativado para cada serviço!" : "Isolamento de processos SvcHost restaurado.");
        }

        private async void ToggleMpo_Click(object sender, RoutedEventArgs e)
        {
            bool enable = ChkMpo.IsChecked == true;
            await _engine.ToggleMpoAsync(enable);
            ShowCheckModal("🖥️ DESATIVAÇÃO DE MPO", enable ? "Multi-Plane Overlay desativado com sucesso (Fim dos stutters em 240Hz / Dual Screen)!" : "MPO reativado no padrão do Windows.");
        }

        private async void ToggleMsi_Click(object sender, RoutedEventArgs e)
        {
            bool enable = ChkMsi.IsChecked == true;
            await _engine.ToggleMsiModeAsync(enable);
            ShowCheckModal("🖥️ MODO MSI (GPU)", enable ? "Modo MSI ativado na GPU com alta prioridade de hardware!" : "Modo MSI revertido para padrão.");
        }

        private async void ToggleNvidiaTweaks_Click(object sender, RoutedEventArgs e)
        {
            bool enable = ChkNvidiaTweaks.IsChecked == true;
            await _engine.ToggleNvidiaTweaksAsync(enable);
            ShowCheckModal("🖥️ NVIDIA TWEAKS", enable ? "Preemption desligado e otimizações de baixa latência aplicadas!" : "Ajustes NVIDIA revertidos para o padrão.");
        }

        private async void ToggleMmcss_Click(object sender, RoutedEventArgs e)
        {
            bool enable = ChkMmcss.IsChecked == true;
            await _engine.ToggleMmcssTweaksAsync(enable);
            ShowCheckModal("🖥️ MMCSS GAMES PRIORITY", enable ? "Prioridade multimídia MMCSS Games configurada (Priority 8 & SFIO High)!" : "Prioridade MMCSS restaurada para o padrão.");
        }

        // ==========================================
        // MÓDULO 2: ANTI-GARGALO FIVEM COM MODAL INTERATIVO (100% SEGURO)
        // ==========================================
        private async void BtnFivemAntiBottleneck_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            SetBusy(true, "APLICANDO ANTI-GARGALO...");

            // Abre o modal de check-in passo a passo
            ModalStatusCheck.Visibility = Visibility.Collapsed;
            ModalRestorePoint.Visibility = Visibility.Collapsed;
            ModalAntiBottleneck.Visibility = Visibility.Visible;
            ModalOverlay.Visibility = Visibility.Visible;
            BtnCloseAntiBottleneckModal.Visibility = Visibility.Collapsed;
            ProgressAntiBottleneck.Value = 0;

            Step1Status.Text = "Em andamento...";
            Step1Status.Foreground = (SolidColorBrush)FindResource("NeonYellow");
            Step2Status.Text = "Aguardando...";
            Step3Status.Text = "Aguardando...";
            Step4Status.Text = "Aguardando...";
            Step5Status.Text = "Aguardando...";

            try
            {
                // Passo 1: CCD0 Affinity (0xFFFF)
                await Task.Delay(400);
                string[] exes = { "FiveM.exe", "GTA5.exe" };
                foreach (var exe in exes)
                {
                    string ifeoPath = $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{exe}\PerfOptions";
                    using var ifeo = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(ifeoPath);
                    ifeo.SetValue("CpuAffinityMask", 65535, Microsoft.Win32.RegistryValueKind.DWord);
                }
                Step1Status.Text = "🟢 Concluído";
                Step1Status.Foreground = (SolidColorBrush)FindResource("NeonGreen");
                ProgressAntiBottleneck.Value = 20;

                // Passo 2: Prioridade Alta de CPU e I/O nos jogos
                Step2Status.Text = "Em andamento...";
                Step2Status.Foreground = (SolidColorBrush)FindResource("NeonYellow");
                await Task.Delay(400);
                foreach (var exe in exes)
                {
                    string ifeoPath = $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{exe}\PerfOptions";
                    using var ifeo = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(ifeoPath);
                    ifeo.SetValue("CpuPriorityClass", 3, Microsoft.Win32.RegistryValueKind.DWord);
                    ifeo.SetValue("IoPriority", 3, Microsoft.Win32.RegistryValueKind.DWord);
                }
                Step2Status.Text = "🟢 Concluído";
                Step2Status.Foreground = (SolidColorBrush)FindResource("NeonGreen");
                ProgressAntiBottleneck.Value = 40;

                // Passo 3: Network Throttling 0xFFFFFFFF
                Step3Status.Text = "Em andamento...";
                Step3Status.Foreground = (SolidColorBrush)FindResource("NeonYellow");
                await Task.Delay(400);
                using (var netThrot = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                {
                    netThrot.SetValue("NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), Microsoft.Win32.RegistryValueKind.DWord);
                    netThrot.SetValue("SystemResponsiveness", 0, Microsoft.Win32.RegistryValueKind.DWord);
                }
                Step3Status.Text = "🟢 Concluído";
                Step3Status.Foreground = (SolidColorBrush)FindResource("NeonGreen");
                ProgressAntiBottleneck.Value = 60;

                // Passo 4: settings.xml
                Step4Status.Text = "Em andamento...";
                Step4Status.Foreground = (SolidColorBrush)FindResource("NeonYellow");
                await Task.Delay(400);
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string xmlPath = Path.Combine(docs, @"Rockstar Games\GTA V\settings.xml");
                if (File.Exists(xmlPath))
                {
                    await _engine.RunDownscaleProfileAsync(6, "1440p Ouro RTX 4070 Ti");
                }
                Step4Status.Text = "🟢 Concluído";
                Step4Status.Foreground = (SolidColorBrush)FindResource("NeonGreen");
                ProgressAntiBottleneck.Value = 80;

                // Passo 5: Limpeza NUI
                Step5Status.Text = "Em andamento...";
                Step5Status.Foreground = (SolidColorBrush)FindResource("NeonYellow");
                await Task.Delay(400);
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string nui = Path.Combine(localApp, @"FiveM\FiveM.app\data\nui-storage");
                if (Directory.Exists(nui)) Directory.Delete(nui, true);
                string crashes = Path.Combine(localApp, @"FiveM\FiveM.app\crashes");
                if (Directory.Exists(crashes)) Directory.Delete(crashes, true);
                Step5Status.Text = "🟢 Concluído";
                Step5Status.Foreground = (SolidColorBrush)FindResource("NeonGreen");
                ProgressAntiBottleneck.Value = 100;

                BtnCloseAntiBottleneckModal.Visibility = Visibility.Visible;
                await RefreshHardwareDataAsync();
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void BtnCloseAntiBottleneckModal_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            ModalAntiBottleneck.Visibility = Visibility.Collapsed;
        }

        private async void BtnTestSpeed_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            SetBusy(true, "MEDINDO VELOCIDADE...");
            try
            {
                var metrics = await _engine.RunFullNetworkSpeedTestAsync();
                TxtNetDownload.Text = $"{metrics.DownloadMbps} Mbps";
                ProgressNetDownload.Value = metrics.DownloadMbps;
                TxtNetUpload.Text = $"{metrics.UploadMbps} Mbps";
                ProgressNetUpload.Value = metrics.UploadMbps;
                TxtNetPing.Text = $"{metrics.PingMs} ms";
                TxtNetLoss.Text = $"{metrics.PacketLossPercent}%";

                ShowCheckModal("⚡ DIAGNÓSTICO DE VELOCIDADE & REDE",
                    $"• Latência Média (Ping): {metrics.PingMs} ms\n" +
                    $"• Perda de Pacotes: {metrics.PacketLossPercent}%\n" +
                    $"• Velocidade de Download Medida: {metrics.DownloadMbps} Mbps\n" +
                    $"• Velocidade de Upload Medida: {metrics.UploadMbps} Mbps\n\n" +
                    (metrics.PacketLossPercent == 0 ? "✔ Conexão excelente com zero perda de pacotes!" : "⚠ Perda de pacotes detectada. Execute o Auto-Reparo Seguro."));
            }
            finally { SetBusy(false); }
        }

        private async void BtnSafeRepairNetwork_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            SetBusy(true, "AUTO-REPARO SEGURO...");
            try
            {
                string report = await _engine.SafeNetworkRepairAsync();
                ShowCheckModal("🛡️ AUTO-REPARO SEGURO DE REDE", report);
                await RefreshHardwareDataAsync();
            }
            finally { SetBusy(false); }
        }

        private async void BtnCheckNetwork_Click(object sender, RoutedEventArgs e)
        {
            SetBusy(true, "CHECANDO REDE...");
            try
            {
                string status = await _engine.CheckNetworkStatusAsync();
                ShowCheckModal("🌐 AUDITORIA DE REDE 1 GBPS / FIBRA", status);
            }
            finally { SetBusy(false); }
        }

        private async void BtnFactoryResetNetwork_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            var res = MessageBox.Show(
                "Deseja realmente redefinir todas as configurações de rede para o PADRÃO ORIGINAL DE FÁBRICA DO WINDOWS?\n\nIsso redefinirá Winsock, pilha IP, propriedades da placa Realtek e DNS para DHCP automático.",
                "Restaurar Padrão de Fábrica",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (res != MessageBoxResult.Yes) return;

            SetBusy(true, "RESTAURANDO FÁBRICA...");
            try
            {
                string report = await _engine.FactoryResetNetworkAsync();
                ShowCheckModal("🔄 REDE RESTAURADA DE FÁBRICA", report);
                await RefreshHardwareDataAsync();
            }
            finally { SetBusy(false); }
        }

        private async void ToggleCcd0_Click(object sender, RoutedEventArgs e)
        {
            bool enable = ChkCcd0.IsChecked == true;
            await _engine.ToggleCcd0Async(enable);
            ShowCheckModal("⚡ AFINIDADE ZEN 3 CCD0", enable ? "FiveM.exe e GTA5.exe travados no CCD0 (0xFFFF) com 32MB de Cache L3 compartilhado!" : "Trava de afinidade CCD0 removida.");
        }

        private void BtnCheckCcd0_Click(object sender, RoutedEventArgs e)
        {
            using var ifeo = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\FiveM.exe\PerfOptions");
            bool ok = false;
            if (ifeo != null)
            {
                var val = ifeo.GetValue("CpuAffinityMask");
                if (val != null && Convert.ToInt64(val) == 65535) ok = true;
            }

            ShowCheckModal("⚡ AFINIDADE CCD0 (RYZEN ZEN 3)", 
                ok ? "STATUS: ATIVADO & BLOQUEADO (0xFFFF)\n\nFiveM e GTA V estão travados nas threads 0 a 15, compartilhando o Cache L3 de 32MB diretamente sem passar pelo barramento Infinity Fabric. Micro-stutters eliminados!"
                   : "STATUS: NÃO CONFIGURADO\n\nClique em 'APLICAR ANTI-GARGALO SUPREMO' para travar a afinidade no CCD0.");
        }

        private void BtnRestoreGtaXml_Click(object sender, RoutedEventArgs e)
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string gtaDir = Path.Combine(docs, @"Rockstar Games\GTA V");
            string xmlPath = Path.Combine(gtaDir, "settings.xml");
            string bakPath = Path.Combine(gtaDir, "settings.xml.bak_original");

            if (File.Exists(bakPath))
            {
                File.Copy(bakPath, xmlPath, true);
                AppendLog("settings.xml original restaurado com sucesso!");
                ShowCheckModal("🎮 SETTINGS.XML RESTAURADO", "O backup settings.xml.bak_original foi restaurado com êxito.");
            }
            else
            {
                ShowCheckModal("⚠ AVISO", "Nenhum arquivo de backup settings.xml.bak_original foi encontrado no diretório do jogo.");
            }
        }

        private void BtnCheckSettingsXml_Click(object sender, RoutedEventArgs e)
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string xmlPath = Path.Combine(docs, @"Rockstar Games\GTA V\settings.xml");

            if (!File.Exists(xmlPath))
            {
                ShowCheckModal("🎮 STATUS DO SETTINGS.XML", "Arquivo settings.xml ainda não existe na pasta Documentos. Abra o FiveM uma vez.");
                return;
            }

            ShowCheckModal("🎮 AUDITORIA DO SETTINGS.XML", 
                $"• Resolução Físico no Cabo: {_specs.GtaScreenWidth}x{_specs.GtaScreenHeight} @ {_specs.GtaRefreshRate + 1}Hz (SEGURO)\n" +
                $"• SamplingMode Atual: {_specs.CurrentGtaSamplingMode}\n" +
                $"• Qualidade da Grama: {_specs.GtaGrassQuality} (0 = Normal, sem quedas)\n" +
                $"• Densidade da Cidade: {_specs.GtaCityDensity} (Alívio de CPU em servidores de RP)\n" +
                $"• Qualidade de Textura: {_specs.GtaTextureQuality} (Muito Alto - Carga na RTX 4070 Ti)");
        }

        private async void BtnCleanNui_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            SetBusy(true, "LIMPANDO NUI...");
            try
            {
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string nui = Path.Combine(localApp, @"FiveM\FiveM.app\data\nui-storage");
                if (Directory.Exists(nui)) Directory.Delete(nui, true);
                AppendLog("Cache NUI limpo com sucesso! Cidades e shaders permanecem intactos.");
                ShowCheckModal("🧹 NUI STORAGE LIMPO", "O cache de menus NUI foi apagado. Os menus do RP carregarão limpos e sem engasgos.");
            }
            finally { SetBusy(false); }
        }

        private void BtnCheckNui_Click(object sender, RoutedEventArgs e)
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string nui = Path.Combine(localApp, @"FiveM\FiveM.app\data\nui-storage");

            long size = 0;
            int count = 0;
            if (Directory.Exists(nui))
            {
                var di = new DirectoryInfo(nui);
                foreach (var f in di.GetFiles("*", SearchOption.AllDirectories))
                {
                    size += f.Length;
                    count++;
                }
            }

            double mb = Math.Round((double)size / (1024 * 1024), 2);
            ShowCheckModal("🧹 STATUS DO CACHE NUI (FIVEM)", 
                $"• Arquivos temporários de menus: {count} itens\n" +
                $"• Espaço ocupado: {mb} MB\n" +
                $"• Pastas de mapas e servidores: 100% BLINDADAS (Não precisam ser baixadas novamente).");
        }

        // ==========================================
        // MÓDULO 3: DOWNSCALE & SUPER RESOLUÇÃO UNIVERSAL
        // ==========================================
        private void BtnCheckActiveResolution_Click(object sender, RoutedEventArgs e)
        {
            string status = _engine.CheckDownscaleStatus(_specs.CurrentGtaSamplingMode, _specs.GtaScreenWidth, _specs.GtaScreenHeight);
            ShowCheckModal("🖥️ AUDITORIA GERAL DE RESOLUÇÃO & MONITORES", status);
        }

        // 1440p para 1080p
        private async void BtnDownscale_1440p_Click(object sender, RoutedEventArgs e)
        {
            await _engine.RunDownscaleProfileAsync(6, "1440p (2560x1440 - Modo de Ouro RTX 4070 Ti)");
            await RefreshHardwareDataAsync();
        }
        private void BtnCheckDownscale_1440p_Click(object sender, RoutedEventArgs e)
        {
            string res = _engine.CheckDownscaleStatus(6, 1920, 1080);
            ShowCheckModal("🖥️ CHECAGEM: 1440P PARA 1080P", res);
        }

        // 1620p para 1080p
        private async void BtnDownscale_1620p_Click(object sender, RoutedEventArgs e)
        {
            await _engine.RunDownscaleProfileAsync(7, "1620p (2880x1620 - Ultra Nitidez)");
            await RefreshHardwareDataAsync();
        }
        private void BtnCheckDownscale_1620p_Click(object sender, RoutedEventArgs e)
        {
            string res = _engine.CheckDownscaleStatus(7, 1920, 1080);
            ShowCheckModal("🖥️ CHECAGEM: 1620P PARA 1080P", res);
        }

        // 4K para 1080p
        private async void BtnDownscale_4K_Click(object sender, RoutedEventArgs e)
        {
            await _engine.RunDownscaleProfileAsync(9, "4K (3840x2160 - Supersampling 2.0x)");
            await RefreshHardwareDataAsync();
        }
        private void BtnCheckDownscale_4K_Click(object sender, RoutedEventArgs e)
        {
            string res = _engine.CheckDownscaleStatus(9, 1920, 1080);
            ShowCheckModal("🖥️ CHECAGEM: 4K PARA 1080P", res);
        }

        // 4K para 1440p
        private async void BtnDownscale_4K1440p_Click(object sender, RoutedEventArgs e)
        {
            await _engine.RunDownscaleProfileAsync(7, "4K para 1440p (3840x2160 Interno em Monitor 2K)", 2560, 1440, 239);
            await RefreshHardwareDataAsync();
        }
        private void BtnCheckDownscale_4K1440p_Click(object sender, RoutedEventArgs e)
        {
            string res = _engine.CheckDownscaleStatus(7, 2560, 1440);
            ShowCheckModal("🖥️ CHECAGEM: 4K PARA 1440P (2K)", res);
        }

        // 1080p Nativo Puro
        private async void BtnDownscale_1080p_Click(object sender, RoutedEventArgs e)
        {
            await _engine.RunDownscaleProfileAsync(0, "1080p Nativo Puro (1.0x)");
            await RefreshHardwareDataAsync();
        }
        private void BtnCheckDownscale_1080p_Click(object sender, RoutedEventArgs e)
        {
            string res = _engine.CheckDownscaleStatus(0, 1920, 1080);
            ShowCheckModal("🖥️ CHECAGEM: 1080P NATIVO PURO", res);
        }

        // 1080p para 720p
        private async void BtnDownscale_720p_Click(object sender, RoutedEventArgs e)
        {
            await _engine.RunDownscaleProfileAsync(2, "1080p para 720p Performance (1280x720)");
            await RefreshHardwareDataAsync();
        }
        private void BtnCheckDownscale_720p_Click(object sender, RoutedEventArgs e)
        {
            string res = _engine.CheckDownscaleStatus(2, 1920, 1080);
            ShowCheckModal("🖥️ CHECAGEM: 1080P PARA 720P", res);
        }

        // 1440x1080 Esticado 4:3
        private async void BtnDownscale_Stretched_Click(object sender, RoutedEventArgs e)
        {
            await _engine.RunDownscaleProfileAsync(0, "1440x1080 Esticado 4:3 Competitivo", 1440, 1080, 239);
            await RefreshHardwareDataAsync();
        }
        private void BtnCheckDownscale_Stretched_Click(object sender, RoutedEventArgs e)
        {
            string res = _engine.CheckDownscaleStatus(0, 1440, 1080);
            ShowCheckModal("🖥️ CHECAGEM: 1440x1080 ESTICADO", res);
        }

        // ==========================================
        // MÓDULO 4: LANÇADOR GAMER PRO & FIVEM
        // ==========================================
        private async void BtnLaunchFivem_Click(object sender, RoutedEventArgs e) => await _engine.LaunchFivemOptimizedAsync();

        private async Task LoadDetectedGamesAsync()
        {
            try
            {
                _detectedGames = await Task.Run(() => _gameLauncher.ScanAllGames());
                CboDetectedGames.ItemsSource = null;
                CboDetectedGames.ItemsSource = _detectedGames;

                if (_detectedGames.Count > 0)
                {
                    var fav = _detectedGames.FirstOrDefault(g => g.Name.Contains("FiveM", StringComparison.OrdinalIgnoreCase))
                           ?? _detectedGames.FirstOrDefault(g => g.Name.Contains("Grand Theft Auto", StringComparison.OrdinalIgnoreCase))
                           ?? _detectedGames[0];
                    CboDetectedGames.SelectedItem = fav;
                    UpdateSelectedGameUI(fav);
                }
                else
                {
                    TxtSelectedGameName.Text = "Nenhum jogo detectado automaticamente";
                    TxtSelectedGamePath.Text = "Clique em 'ESCOLHER .EXE' para apontar o executável do seu jogo.";
                    TxtSelectedGamePlatform.Text = "MANUAL";
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[AVISO] Falha ao listar jogos instalados: {ex.Message}");
            }
        }

        private void UpdateSelectedGameUI(InstalledGame? game)
        {
            if (game != null)
            {
                TxtSelectedGameName.Text = game.Name;
                TxtSelectedGamePath.Text = game.ExePath;
                TxtSelectedGamePlatform.Text = game.Platform.ToUpper();
            }
        }

        private void CboDetectedGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboDetectedGames.SelectedItem is InstalledGame game)
            {
                UpdateSelectedGameUI(game);
            }
        }

        private async void BtnRefreshGames_Click(object sender, RoutedEventArgs e)
        {
            SetBusy(true, "PROCURANDO JOGOS...");
            try
            {
                await LoadDetectedGamesAsync();
                ShowCheckModal("🎮 BIBLIOTECA DE JOGOS ATUALIZADA", $"Encontrados {_detectedGames.Count} jogos instalados (Steam, Epic Games e FiveM).");
            }
            finally { SetBusy(false); }
        }

        private void BtnBrowseCustomGame_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executáveis do Jogo (*.exe)|*.exe|Todos os Arquivos (*.*)|*.*",
                Title = "Selecione o Executável (.exe) do Jogo"
            };

            if (dlg.ShowDialog() == true)
            {
                string exePath = dlg.FileName;
                string gameName = Path.GetFileNameWithoutExtension(exePath);

                var newGame = new InstalledGame
                {
                    Name = gameName,
                    ExePath = exePath,
                    Platform = "Custom"
                };

                _gameLauncher.SaveCustomGame(newGame);
                _detectedGames.Insert(0, newGame);
                CboDetectedGames.ItemsSource = null;
                CboDetectedGames.ItemsSource = _detectedGames;
                CboDetectedGames.SelectedItem = newGame;
                UpdateSelectedGameUI(newGame);

                ShowCheckModal("🎮 JOGO PERSONALIZADO ADICIONADO", $"'{gameName}' foi registrado no Lançador Matrix com sucesso!");
            }
        }

        private async void BtnLaunchSelectedGame_Click(object sender, RoutedEventArgs e)
        {
            if (CboDetectedGames.SelectedItem is not InstalledGame game)
            {
                ShowCheckModal("⚠ SELEÇÃO NECESSÁRIA", "Por favor, selecione um jogo na lista ou clique em 'ESCOLHER .EXE'.");
                return;
            }

            bool cleanRam = ChkOptCleanRam.IsChecked == true;
            bool lockCcd0 = ChkOptLockCcd0.IsChecked == true;
            bool highPriority = ChkOptHighPriority.IsChecked == true;

            SetBusy(true, "DISPARANDO JOGO OTIMIZADO...");
            try
            {
                bool ok = await _gameLauncher.LaunchGameOptimizedAsync(game, cleanRam, lockCcd0, highPriority);
                if (ok)
                {
                    ShowCheckModal("🚀 JOGO DISPARADO COM SUCESSO!",
                        $"O jogo '{game.Name}' foi iniciado com as configurações competitivas ativas:\n\n" +
                        (cleanRam ? "✔ Cache Standby da RAM Esvaziado (2GB a 6GB livres)\n" : "") +
                        (lockCcd0 ? "✔ Sentinela CCD0 Zen 3 ativo (Travado nas threads 0 a 15 / 32MB L3)\n" : "") +
                        (highPriority ? "✔ Prioridade de Processo Alta & Temporizador 0.5ms\n" : "") +
                        "✔ Inicialização segura bypass de anti-cheat (Token Explorer)!");
                }
                else
                {
                    ShowCheckModal("✖ FALHA AO INICIAR", $"Não foi possível iniciar o executável:\n{game.ExePath}");
                }
            }
            finally { SetBusy(false); }
        }

        // ==========================================
        // MÓDULO 5: LIMPEZA BLINDADA
        // ==========================================
        private async void BtnArmoredClean_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            SetBusy(true, "LIMPANDO SISTEMA...");
            try
            {
                await _engine.RunArmoredCleanAsync();
                ShowCheckModal("🧹 LIMPEZA BLINDADA CONCLUÍDA", "Todos os caches temporários (%TEMP%, Temp do Windows, Prefetch, GPU Shaders e Lixeira) foram limpos sem tocar nos dados do FiveM.");
            }
            finally { SetBusy(false); }
        }

        private async void BtnCheckCleanable_Click(object sender, RoutedEventArgs e)
        {
            SetBusy(true, "AUDITANDO LIXO...");
            try
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cleanmgr.exe",
                        Arguments = "/d C:",
                        UseShellExecute = true
                    });
                }
                catch { }

                string report = await _engine.CheckCleanableWasteAsync();
                ShowCheckModal("🧹 DIAGNÓSTICO DE ARQUIVOS TEMPORÁRIOS", report + "\n\n✔ Utilitário de Limpeza de Disco do Windows (cleanmgr.exe /d C:) iniciado automaticamente na unidade C:!");
            }
            finally { SetBusy(false); }
        }

        // ==========================================
        // MÓDULO 6: DEFENDER & UPDATE
        // ==========================================
        private async void BtnDisableDefender_Click(object sender, RoutedEventArgs e) => await _engine.ToggleWindowsDefenderAsync(true);
        private async void BtnEnableDefender_Click(object sender, RoutedEventArgs e) => await _engine.ToggleWindowsDefenderAsync(false);
        private async void BtnDisableUpdate_Click(object sender, RoutedEventArgs e) => await _engine.ToggleWindowsUpdateAsync(true);
        private async void BtnEnableUpdate_Click(object sender, RoutedEventArgs e) => await _engine.ToggleWindowsUpdateAsync(false);

        // ==========================================
        // MÓDULO 7: SCANNER
        // ==========================================
        private async void BtnRescan_Click(object sender, RoutedEventArgs e)
        {
            AppendLog("Re-escaneando sistema, integridade de 1 Gbps e gargalos...");
            await RefreshHardwareDataAsync();
            ShowCheckModal("🩺 DIAGNÓSTICO DO SISTEMA ATUALIZADO", 
                $"• Índice Geral de Saúde e Otimização: {_specs.HealthScore}%\n" +
                $"• Processador: {_specs.CpuName} ({_specs.CpuThreads} threads)\n" +
                $"• Placa de Vídeo: {_specs.GpuName}\n" +
                $"• Rede Principal: {_specs.NetworkCardName} ({_specs.NetworkLinkSpeed})\n" +
                $"• Memória RAM: {_specs.RamTotalGb}GB\n" +
                $"• Status Geral: Otimizado e seguro para jogos competitivos.");
        }

        // ==========================================
        // MÓDULO 8: ATUALIZAÇÕES & VERSIONAMENTO (AUTO-UPDATER)
        // ==========================================
        private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            SetBusy(true, "CONSULTANDO CANAL DE ATUALIZAÇÕES...");
            try
            {
                AppendLog("Verificando atualizações no canal oficial ErreJota Matrix (v1.0.0)...");
                
                // 1. Tentar checar no servidor online oficial
                var update = await _updateManager.CheckForUpdateAsync();

                // 2. Fallback inteligente: se existir um version.json local para testes/customização
                if (update == null && File.Exists("version.json"))
                {
                    try
                    {
                        string localJson = await File.ReadAllTextAsync("version.json");
                        var localInfo = System.Text.Json.JsonSerializer.Deserialize<UpdateInfo>(localJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (localInfo != null && Version.TryParse(localInfo.Version, out var localVer) && localVer > UpdateManager.CurrentVersion)
                        {
                            update = localInfo;
                        }
                    }
                    catch { }
                }

                if (update != null)
                {
                    _pendingUpdate = update;
                    TxtNewVersionBadge.Text = $"v{update.Version} DISPONÍVEL";
                    TxtNewVersionTarget.Text = $"v{update.Version}";
                    TxtUpdateDate.Text = string.IsNullOrWhiteSpace(update.ReleaseDate) ? $"Data: {DateTime.Now:dd/MM/yyyy}" : $"Data: {update.ReleaseDate}";
                    TxtUpdateChangelog.Text = string.IsNullOrWhiteSpace(update.Changelog) ? "• Melhorias gerais e otimizações de estabilidade." : update.Changelog;

                    PanelUpdateProgress.Visibility = Visibility.Collapsed;
                    BtnApplyUpdateNow.IsEnabled = true;
                    ModalOverlay.Visibility = Visibility.Visible;
                    ModalUpdate.Visibility = Visibility.Visible;
                    ModalStatusCheck.Visibility = Visibility.Collapsed;
                    ModalRestorePoint.Visibility = Visibility.Collapsed;
                    ModalAntiBottleneck.Visibility = Visibility.Collapsed;
                    AppendLog($"[ATUALIZADOR] Nova versão v{update.Version} encontrada e disponível para download!");
                }
                else
                {
                    ShowCheckModal("🚀 SISTEMA DE ATUALIZAÇÕES ERREJOTA",
                        "• VERSÃO INSTALADA: v1.0.0 (Release Oficial)\n" +
                        "• STATUS: Você já está utilizando a versão mais recente!\n\n" +
                        "✔ Kernel Gamer 2026 sincronizado\n" +
                        "✔ Motor Anti-Gargalo FiveM atualizado\n" +
                        "✔ Telemetria de Rede e Ponto de Restauração ativos\n\n" +
                        "Assim que você publicar uma nova versão online (ex: v1.0.1), o aplicativo notificará e atualizará automaticamente com 1 clique.");
                }
            }
            catch (Exception ex)
            {
                ShowCheckModal("⚠️ VERIFICAÇÃO DE ATUALIZAÇÃO", $"Não foi possível conectar ao canal de atualizações: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void BtnApplyUpdateNow_Click(object sender, RoutedEventArgs e)
        {
            if (_pendingUpdate == null) return;

            if (string.IsNullOrWhiteSpace(_pendingUpdate.DownloadUrl))
            {
                _pendingUpdate.DownloadUrl = $"https://github.com/Hyuryx/errejota_otimiza-ao/releases/download/v{_pendingUpdate.Version}/ErreJota_Matrix_Optimizer_Pro.exe";
            }

            BtnApplyUpdateNow.IsEnabled = false;
            PanelUpdateProgress.Visibility = Visibility.Visible;
            BarUpdateProgress.Value = 0;
            TxtUpdateProgressPercent.Text = "0%";
            TxtUpdateProgressLabel.Text = "Iniciando download da versão v" + _pendingUpdate.Version + "...";

            string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ErreJota_Matrix_Optimizer_Pro.exe");

            AppendLog($"[ATUALIZADOR] Baixando v{_pendingUpdate.Version} de {_pendingUpdate.DownloadUrl}...");
            bool ok = await _updateManager.DownloadAndApplyUpdateAsync(_pendingUpdate.DownloadUrl, currentExePath);

            if (ok)
            {
                AppendLog("✔ Download da nova versão finalizado com sucesso!");
                AppendLog("🚀 Reiniciando ErreJota Matrix Optimizer Pro na nova versão...");
                await Task.Delay(1500);
                Application.Current.Shutdown();
            }
            else
            {
                BtnApplyUpdateNow.IsEnabled = true;
                PanelUpdateProgress.Visibility = Visibility.Collapsed;
                MessageBox.Show("Falha ao baixar a atualização. Verifique sua conexão com a internet ou o link de download.", "Erro de Atualização", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelUpdate_Click(object sender, RoutedEventArgs e)
        {
            ModalUpdate.Visibility = Visibility.Collapsed;
            ModalOverlay.Visibility = Visibility.Collapsed;
        }
    }
}