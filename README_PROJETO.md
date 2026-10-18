# ⚡ ERREJOTA MATRIX OPTIMIZER PRO
> **Painel Supremo de Otimização, Anti-Gargalo e Kernel Gamer**  
> *© 2026 ERREJOTA. Todos os direitos reservados.*

---

## 📌 Visão Geral da Arquitetura
O **ErreJota Matrix Optimizer Pro** foi desenvolvido em **C# moderno (.NET 10)** com interface **WPF acelerada por hardware DirectX** nativa da GPU.  
A arquitetura foi desenhada em camadas modulares para permitir a adição contínua de novas abas, rotinas de otimização e integrações de jogos nos próximos upgrades.

---

## 🎛️ Módulos Integrados no Painel (Layout Cheat Loader // Sidebar & Cards)

### 1. 🚀 Aba "Geral & Kernel"
- **Inicialização Segura:** Todos os cards iniciam desativados por padrão para total transparência e consentimento do usuário.
- **Controle Individual ou Mestre:**
  - Cada toggle/card pode ser ativado e revertido individualmente.
  - O botão mestre **"EXECUTAR SUPREMO (1-CLIQUE)"** ativa todos os módulos em conjunto com feedback visual imediato.
- **Cards Flutuantes com Toggles Independentes:**
  - **Plano de Energia:** Ativação instantânea do Desempenho Máximo (Ultimate Performance).
  - **Unpark CPU:** Desestacionamento de 100% dos núcleos lógicos da CPU.
  - **Win32PrioritySeparation 40 (0x28):** Foco quântico de prioridade máxima no jogo em primeiro plano.
  - **Isolamento de Processos SvcHost Split:** Evita congestionamento de memória entre serviços.
  - **Desativação de MPO (Multi-Plane Overlay):** Fim dos engasgos e stutters em múltiplos monitores 240Hz.
  - **Modo MSI (Message Signaled Interrupts):** Interrupções diretas de hardware em Alta Prioridade para GPU.
  - **NVIDIA Tweaks Extremos:** Preemption desligada (`DisablePreemption 1`), HAGS 2 e latências em 1ms.
  - **MMCSS Games:** Prioridade de GPU 8, SFIO High e Latency Sensitive True.
- **Restauração de Segurança:** Criar Ponto de Restauração ou Voltar aos Padrões de Fábrica do Windows.

---

### 2. ⚡ Aba "Anti-Gargalo FiveM & FPS"
- **Trava de Afinidade CCD0 (`0xFFFF` / 65535 decimal):**
  - Conecta o FiveM e GTA V exclusivamente nos 8 núcleos de alta performance do CCD0 com 32MB de cache L3 unificado.
- **Otimização Segura de Sistema para Jogos:**
  - Prioridade Alta de CPU e I/O nos jogos (IFEO).
  - Throttling multimídia do Windows desativado (`0xFFFFFFFF`).
  - **Desacoplamento de Rede:** Não altera drivers, placas ou configurações destrutivas de rede durante a otimização dos jogos.
- **Desgargalo do `settings.xml` do GTA V:**
  - Grama Normal (GrassQuality 0).
  - Densidade da Cidade 0.2 (alívio de CPU para servidores de RP cheios).
  - Texturas no Muito Alto (trabalho direto na GPU).
- **Limpeza de Cache NUI:**
  - Limpa `data\nui-storage` e `crashes` preservando 100% de cidades e servidores.
- **Painel de Telemetria e Auto-Reparo Seguro de Rede (Microsoft Recommended):**
  - **Telemetria ao Vivo:** Medição de Download (Mbps), Upload (Mbps), Ping (ms) e Perda de Pacotes (%).
  - **Auto-Reparo Seguro:** Execução dos comandos recomendados pelo suporte oficial da Microsoft (`ipconfig /flushdns`, `arp -d *`, `ipconfig /registerdns`, `ipconfig /release & renew`, `netsh winsock reset`, `netsh int ip reset`).
  - **Preservação:** Mantém intactos MTU, adaptadores, drivers, proxies e regras de firewall.

---

### 3. 🖥️ Aba "Super Resolução & Downscale Universal"
- **Seletor de Resoluções Internas:**
  - 1440p (2560x1440 - Modo 4/3) [Recomendado RTX 4070 Ti]
  - 1620p (2880x1620 - Modo 3/2) [Ultra Nitidez]
  - 4K (3840x2160 - Modo 2/1) [Supersampling 2.0x]
  - 1080p Nativo Puro (1.0x) [Modo Competitivo]
  - 720p Performance (1280x720) [Downscale Máximo FPS]
- **Sinal Físico Protegido:** Mantém a saída dos cabos HDMI/DP em **1920x1080 @ 240Hz**, eliminando qualquer risco de desligamento dos monitores Samsung Odyssey.

---

### 4. 🎮 Aba "Lançador FiveM"
- Inicialização sem privilégios de Administrador (blindagem contra erro de anti-cheat).
- Esvaziamento e compactação da memória RAM em espera antes de abrir o jogo.
- Thread Sentinela que aplica a Afinidade CCD0 e Prioridade Alta em tempo real.

---

### 5. 🧹 Aba "Limpeza Blindada"
- Limpeza inteligente de arquivos temporários (`%TEMP%`, Temp do Windows, crash dumps, logs antigos e cache de navegadores).
- **Blindagem Absoluta:** Shaders DirectX/NVIDIA e dados de servidores FiveM protegidos.

---

### 6. 🛡️ Aba "Defender & Updates"
- **Desativar 100% Windows Defender:** Integração direta com `dfControl.exe` e chaves de registro profundas.
- **Bloquear 100% Windows Update:** Integração com `Wub_x64.exe` e desativação de serviços de telemetria de update.

---

### 7. 🩺 Aba "Scanner de Gargalos"
- Diagnóstico em tempo real com lâmpadas indicadoras (🟢 Otimizado / 🔴 Gargalo Detectado) e botão para re-escanear.

---

### 8. 📋 Aba "Documentação"
- Manual técnico e memória de cálculo de todos os tweaks.

---

### 2. ⚡ Aba "Anti-Gargalo FiveM & FPS"
- **Trava de Afinidade CCD0 (0xFFFF / 65535 decimal):**
  - Trava o FiveM e o GTA V nos 8 núcleos físicos de alta performance do CCD0 do Ryzen 9 5950X.
  - Compartilha o cache unificado de 32MB L3 sem atravessar o Infinity Fabric, eliminando ~80ns de latência inter-CCD e acabando com os micro-stutters.
- **Correção de Perda de Pacotes (PL 5% -> 0%):**
  - Eleva buffers da placa de rede (`*ReceiveBuffers` e `*TransmitBuffers`) para **2048 pacotes**.
  - Zera o algoritmo de Nagle (`TcpAckFrequency = 1`, `TCPNoDelay = 1`).
  - Desativa o throttling de rede multimídia do Windows (`NetworkThrottlingIndex = 0xFFFFFFFF`, `SystemResponsiveness = 0`).
- **Desgargalo do `settings.xml` do GTA V:**
  - Grama Normal (`GrassQuality = 0`).
  - Densidade da Cidade `0.2` (libera a CPU para os players do RP).
  - Reflection MSAA desligado.
  - Texturas no Muito Alto (`TextureQuality = 2`) para a RTX 4070 Ti trabalhar em 85-95%.
- **Limpeza NUI Segura:**
  - Limpa pastas de interface (`data\nui-storage` e `crashes`) mantendo 100% blindados os arquivos de cidades, mapas e servidores.
- **Disparador de Teste de Packet Loss ao Vivo:**
  - Teste de 25 pacotes contínuos com log direto no terminal do painel.

---

### 3. 🖥️ Aba "Super Resolução & Downscale Universal"
- **Perfis de Supersampling Interno:**
  - **1440p para 1080p (SamplingMode 6 - 4/3):** O modo de ouro para a RTX 4070 Ti em tela de 1080p.
  - **1620p para 1080p (SamplingMode 7 - 3/2):** Ultra nitidez para jogos de história e campanha.
  - **4K para 1080p (SamplingMode 9 - 2/1):** Supersampling máximo 2.0x.
  - **1080p Nativo Puro (SamplingMode 0 - 1.0x):** Foco total em FPS bruto competitivo.
  - **720p Performance (SamplingMode 2 - 0.66x):** Downscale extremo.
- **Segurança Absoluta dos Monitores:**
  - O sinal físico HDMI/DP permanece fixado em **1920x1080 @ 240Hz**, eliminando qualquer risco de tela preta, monitor fora de escala ou desligamento dos Samsung Odyssey.

---

### 4. 🎮 Aba "Lançador FiveM"
- Inicializa o FiveM como usuário regular, evitando o bloqueio de anti-cheat de "processo elevado".
- Limpa a memória RAM em espera antes de abrir o jogo (`EmptyWorkingSet`).
- Dispara uma **Thread Sentinela** em segundo plano que monitora a abertura do jogo e acopla a Afinidade CCD0 e Prioridade Alta em tempo real.

---

### 5. 🧹 Aba "Limpeza Blindada"
- Limpeza inteligente de arquivos temporários do sistema, `%TEMP%`, `C:\Windows\Temp` e crash dumps.
- **Blindagem Ativa:** Shaders DirectX/NVIDIA e dados de servidores FiveM protegidos contra exclusão acidental.

---

### 6. 🩺 Aba "Scanner de Gargalos"
- Diagnóstico em tempo real de hardware, status de MPO, timers de kernel, Win32PrioritySeparation e adaptadores de rede.

---

### 7. 📋 Aba "Módulos & Documentação"
- Resumo técnico interativo embutido para consulta rápida do usuário.

---

## 🛠️ Executáveis Gerados
1. **`ErreJota_Matrix_Optimizer_Pro.exe`** (~580 KB):
   - Localizado em `C:\Users\ErreJota\Desktop\` e `C:\Users\ErreJota\Desktop\base\`.
   - Execução instantânea e ultraleve com inicialização acelerada por GPU.
2. **`ErreJota_Matrix_Optimizer_Universal.exe`** (~140 MB):
   - Localizado em `C:\Users\ErreJota\Desktop\base\`.
   - Versão autônoma (*Self-Contained*): contém todo o runtime embutido para você enviar para qualquer amigo ou cliente sem precisar instalar nada no PC dele.

---

*© 2026 ERREJOTA. Todos os direitos reservados.*
