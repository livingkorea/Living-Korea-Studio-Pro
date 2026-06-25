using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using Whisper.net;
using Drawing = System.Drawing;

namespace LivingKoreaStudio;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public class MainForm : Form
{
    private TextBox projectBox = new();
    private TextBox koreanBox = new();
    private TextBox englishBox = new();
    private TextBox memoBox = new();
    private Label statusLabel = new();
    private ComboBox styleBox = new();
    private Button micButton = new();
    private Button micCheckButton = new();
    private Button themeButton = new();
    private Button diagnoseButton = new();

    private Panel headerPanel = new();
    private bool darkMode = false;
    private bool isRecording = false;
    private bool autoMicMode = false;
    private bool isTranscribing = false;
    private CancellationTokenSource? micLoopCts;
    private const int SegmentSeconds = 5;

    private WaveInEvent? waveIn;
    private WaveFileWriter? writer;
    private string? currentWavPath;

    private readonly Drawing.Color Blue = Drawing.Color.FromArgb(11, 85, 217);
    private readonly Drawing.Color Navy = Drawing.Color.FromArgb(7, 27, 85);
    private readonly Drawing.Color LightBg = Drawing.Color.FromArgb(239, 246, 255);
    private readonly Drawing.Color LightCard = Drawing.Color.White;
    private readonly Drawing.Color DarkBg = Drawing.Color.FromArgb(17, 24, 39);
    private readonly Drawing.Color DarkCard = Drawing.Color.FromArgb(31, 41, 55);
    private readonly Drawing.Color DarkText = Drawing.Color.FromArgb(243, 244, 246);

    private const string ModelUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin";

    public MainForm()
    {
        Text = "Living Korea Studio Pro";
        Width = 1240;
        Height = 880;
        MinimumSize = new Drawing.Size(1080, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BuildUI();
        ApplyTheme();
    }

    private void BuildUI()
    {
        Font = new Drawing.Font("맑은 고딕", 10);
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(16),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        Controls.Add(root);

        headerPanel = new Panel { BackColor = Blue, Dock = DockStyle.Fill, Padding = new Padding(10), Margin = new Padding(0, 0, 0, 10) };
        headerPanel.Controls.Add(new Label
        {
            Text = "Living Korea Studio Pro",
            ForeColor = Drawing.Color.White,
            Font = new Drawing.Font("Arial", 24, Drawing.FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(18, 9, 0, 0)
        });
        headerPanel.Controls.Add(new Label
        {
            Text = "로컬 Whisper 자동 자막 · 영어 번역 · 유튜브 제작 메모장  /  Local Whisper Auto Caption · Translation · Notes",
            ForeColor = Drawing.Color.White,
            Font = new Drawing.Font("맑은 고딕", 10),
            Dock = DockStyle.Bottom,
            Height = 34,
            Padding = new Padding(20, 0, 0, 10)
        });
        root.Controls.Add(headerPanel, 0, 0);

        var topCard = CardPanel();
        var top = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(14, 12, 14, 8) };
        top.Controls.Add(new Label { Text = "프로젝트 / Project", Width = 120, Height = 32, TextAlign = Drawing.ContentAlignment.MiddleLeft });
        projectBox.Width = 230;
        projectBox.PlaceholderText = "예: 집구하기 / Housing";
        top.Controls.Add(projectBox);

        top.Controls.Add(new Label { Text = "번역 스타일 / Style", Width = 130, Height = 32, TextAlign = Drawing.ContentAlignment.MiddleLeft });
        styleBox.Width = 240;
        styleBox.DropDownStyle = ComboBoxStyle.DropDownList;
        styleBox.Items.AddRange(new object[]
        {
            "유튜브 대본용 / YouTube Script",
            "자연스러운 영어 / Natural English",
            "쉬운 영어 / Simple English",
            "정중한 설명체 / Formal"
        });
        styleBox.SelectedIndex = 0;
        top.Controls.Add(styleBox);

        themeButton.Text = "🌙 다크모드 / Dark";
        themeButton.Width = 145;
        themeButton.Height = 34;
        themeButton.Click += (_, _) => { darkMode = !darkMode; ApplyTheme(); };
        top.Controls.Add(themeButton);

        statusLabel.Text = "대기 중 / Ready";
        statusLabel.AutoSize = true;
        statusLabel.Padding = new Padding(14, 8, 0, 0);
        top.Controls.Add(statusLabel);

        topCard.Controls.Add(top);
        root.Controls.Add(topCard, 0, 1);

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 590 };
        var inputCard = CardPanel();
        var outputCard = CardPanel();

        koreanBox.Multiline = true;
        koreanBox.ScrollBars = ScrollBars.Vertical;
        koreanBox.Font = new Drawing.Font("맑은 고딕", 12);
        koreanBox.Dock = DockStyle.Fill;
        koreanBox.BorderStyle = BorderStyle.FixedSingle;
        koreanBox.PlaceholderText = "여기에 한국어를 입력하거나 마이크 시작을 누르고 말하세요.\r\nType Korean here or press Start Mic and speak.";

        englishBox.Multiline = true;
        englishBox.ScrollBars = ScrollBars.Vertical;
        englishBox.Font = new Drawing.Font("Arial", 12);
        englishBox.Dock = DockStyle.Fill;
        englishBox.BorderStyle = BorderStyle.FixedSingle;
        englishBox.ForeColor = Drawing.Color.FromArgb(11, 47, 128);
        englishBox.PlaceholderText = "영어 번역 결과가 여기에 표시됩니다.\r\nEnglish translation will appear here.";

        inputCard.Controls.Add(WrapWithLabel("🇰🇷 한국어 입력 / Korean Input", koreanBox));
        outputCard.Controls.Add(WrapWithLabel("🇺🇸 영어 번역 / English Translation", englishBox));
        split.Panel1.Controls.Add(inputCard);
        split.Panel2.Controls.Add(outputCard);
        root.Controls.Add(split, 0, 2);

        var buttonCard = CardPanel();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12, 12, 12, 8) };

        micButton = MakeButton("🎤 마이크 시작 / Start Mic", 210);
        micButton.Click += async (_, _) => await ToggleRecordingAsync();

        micCheckButton = MakeButton("🔎 마이크 확인 / Check Mic", 180);
        micCheckButton.Click += (_, _) => CheckMic();

        var translateButton = MakeButton("🌎 번역하기 / Translate", 170);
        translateButton.Click += async (_, _) => await TranslateAsync();

        diagnoseButton = MakeButton("🧪 환경진단 / Diagnose", 180);
        diagnoseButton.Click += (_, _) => RunEnvironmentDiagnostics();

        var addMemoButton = MakeButton("📝 메모 추가 / Add Note", 165);
        addMemoButton.Click += (_, _) => AddToMemo();

        var copyButton = MakeButton("📋 둘 다 복사 / Copy Both", 170);
        copyButton.Click += (_, _) => CopyBoth();

        var saveButton = MakeButton("💾 TXT 저장 / Save", 140);
        saveButton.Click += (_, _) => SaveTxt();

        var clearButton = MakeButton("🧹 비우기 / Clear", 130);
        clearButton.Click += (_, _) => { koreanBox.Clear(); englishBox.Clear(); SetStatus("입력창을 비웠습니다. / Cleared"); };

        buttons.Controls.AddRange(new Control[] { micButton, micCheckButton, diagnoseButton, translateButton, addMemoButton, copyButton, saveButton, clearButton });
        buttonCard.Controls.Add(buttons);
        root.Controls.Add(buttonCard, 0, 3);

        var memoCard = CardPanel();
        memoBox.Multiline = true;
        memoBox.ScrollBars = ScrollBars.Vertical;
        memoBox.Font = new Drawing.Font("맑은 고딕", 10);
        memoBox.Dock = DockStyle.Fill;
        memoBox.BorderStyle = BorderStyle.FixedSingle;
        memoBox.PlaceholderText = "저장한 번역 메모가 여기에 쌓입니다.\r\nSaved translation notes will appear here.";
        memoCard.Controls.Add(WrapWithLabel("📝 번역 메모장 / Translation Notes", memoBox));
        root.Controls.Add(memoCard, 0, 4);
    }

    private Panel CardPanel() => new Panel { BackColor = LightCard, Padding = new Padding(10), Margin = new Padding(0, 0, 0, 10), Dock = DockStyle.Fill };

    private Button MakeButton(string text, int width) => new Button
    {
        Text = text,
        Width = width,
        Height = 38,
        FlatStyle = FlatStyle.Flat,
        Font = new Drawing.Font("맑은 고딕", 9, Drawing.FontStyle.Bold)
    };

    private Control WrapWithLabel(string label, Control inner)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(8) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            Font = new Drawing.Font("맑은 고딕", 11, Drawing.FontStyle.Bold),
            TextAlign = Drawing.ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 0, 0)
        }, 0, 0);
        panel.Controls.Add(inner, 0, 1);
        return panel;
    }

    private void ApplyTheme()
    {
        var bg = darkMode ? DarkBg : LightBg;
        var card = darkMode ? DarkCard : LightCard;
        var text = darkMode ? DarkText : Drawing.Color.FromArgb(17, 24, 39);
        var boxBg = darkMode ? Drawing.Color.FromArgb(17, 24, 39) : Drawing.Color.White;
        var boxText = darkMode ? Drawing.Color.White : Drawing.Color.Black;

        BackColor = bg;
        foreach (Control c in Controls) ApplyThemeToControl(c, bg, card, text, boxBg, boxText);

        headerPanel.BackColor = darkMode ? Navy : Blue;
        themeButton.Text = darkMode ? "☀ 라이트 / Light" : "🌙 다크모드 / Dark";
        englishBox.ForeColor = darkMode ? Drawing.Color.FromArgb(147, 197, 253) : Drawing.Color.FromArgb(11, 47, 128);
    }

    private void ApplyThemeToControl(Control control, Drawing.Color bg, Drawing.Color card, Drawing.Color text, Drawing.Color boxBg, Drawing.Color boxText)
    {
        if (control is TextBox tb)
        {
            tb.BackColor = boxBg;
            tb.ForeColor = boxText;
        }
        else if (control is Panel p && p != headerPanel)
        {
            p.BackColor = card;
        }
        else if (control is Label lbl && lbl.Parent != headerPanel)
        {
            lbl.ForeColor = text;
            lbl.BackColor = lbl.Parent?.BackColor ?? bg;
        }
        else if (control is Button btn)
        {
            btn.BackColor = darkMode ? Drawing.Color.FromArgb(55, 65, 81) : Drawing.Color.FromArgb(239, 246, 255);
            btn.ForeColor = darkMode ? Drawing.Color.White : Navy;
            btn.FlatAppearance.BorderColor = darkMode ? Drawing.Color.FromArgb(75, 85, 99) : Drawing.Color.FromArgb(191, 219, 254);
        }
        else if (control is ComboBox cb)
        {
            cb.BackColor = boxBg;
            cb.ForeColor = boxText;
        }
        else
        {
            control.BackColor = control is TableLayoutPanel or FlowLayoutPanel ? bg : control.BackColor;
        }

        foreach (Control child in control.Controls) ApplyThemeToControl(child, bg, card, text, boxBg, boxText);
    }


    private void SetStatus(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetStatus(text)));
            return;
        }

        statusLabel.Text = text;
        statusLabel.Refresh();
    }

    private async Task ToggleRecordingAsync()
    {
        if (!autoMicMode)
        {
            autoMicMode = true;
            micLoopCts = new CancellationTokenSource();
            micButton.Text = "🛑 마이크 정지 / Stop Mic";
            SetStatus("마이크 ON. 말하면 5초 단위로 자동 자막과 번역을 만듭니다. / Mic ON. Auto caption every 5 seconds.");
            _ = RunAutoMicLoopAsync(micLoopCts.Token);
        }
        else
        {
            autoMicMode = false;
            micLoopCts?.Cancel();
            StopRecording(false);
            micButton.Text = "🎤 마이크 시작 / Start Mic";
            SetStatus("마이크 정지 / Mic stopped");
            await Task.CompletedTask;
        }
    }

    private async Task RunAutoMicLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                StartRecording(false);
                await Task.Delay(TimeSpan.FromSeconds(SegmentSeconds), token);
                var wavPath = StopRecording(false);
                await Task.Delay(300);

                if (!string.IsNullOrWhiteSpace(wavPath) && File.Exists(wavPath) && !isTranscribing)
                {
                    isTranscribing = true;
                    await TranscribeWavAsync(wavPath);
                    isTranscribing = false;
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                isTranscribing = false;
                SetStatus("마이크 자동 처리 오류 / Auto mic error");
                SaveErrorLog("Auto microphone loop failed", ex);
                await Task.Delay(1000);
            }
        }
    }

    private void StartRecording(bool updateButton = true)
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "LivingKoreaStudio");
            Directory.CreateDirectory(tempDir);
            currentWavPath = Path.Combine(tempDir, "recording_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav");

            waveIn = new WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(16000, 16, 1)
            };

            writer = new WaveFileWriter(currentWavPath, waveIn.WaveFormat);
            waveIn.DataAvailable += (_, e) =>
            {
                writer?.Write(e.Buffer, 0, e.BytesRecorded);
                writer?.Flush();
            };
            waveIn.RecordingStopped += (_, _) =>
            {
                writer?.Dispose();
                writer = null;
                waveIn?.Dispose();
                waveIn = null;
            };

            waveIn.StartRecording();
            isRecording = true;
            if (updateButton) micButton.Text = "🛑 마이크 정지 / Stop Mic";
            SetStatus("듣는 중... 한국어로 말하세요. / Listening... Please speak Korean.");
        }
        catch (Exception ex)
        {
            SetStatus("녹음 오류 / Recording error");
            MessageBox.Show("마이크 녹음을 시작할 수 없습니다.\nCould not start microphone recording.\n\n" + ex.Message, "녹음 오류 / Recording Error");
        }
    }

    private string? StopRecording(bool updateButton = true)
    {
        try
        {
            var wavPath = currentWavPath;
            isRecording = false;
            if (updateButton) micButton.Text = "🎤 마이크 시작 / Start Mic";
            waveIn?.StopRecording();
            SetStatus("음성 조각 변환 준비 중... / Preparing Whisper segment...");
            return wavPath;
        }
        catch (Exception ex)
        {
            SetStatus("녹음 중지 오류 / Stop recording error");
            MessageBox.Show(ex.Message, "녹음 오류 / Recording Error");
            return null;
        }
    }


    private void RunEnvironmentDiagnostics()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var runtimeNativeDir = Path.Combine(baseDir, "runtimes", "win-x64", "native");
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LivingKoreaStudio");
            var modelPath = Path.Combine(appData, "models", "ggml-tiny.bin");
            var logDir = Path.Combine(appData, "logs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "diagnostics_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");

            var sb = new StringBuilder();
            sb.AppendLine("Living Korea Studio Diagnostics");
            sb.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("App base directory: " + baseDir);
            sb.AppendLine("OS: " + Environment.OSVersion);
            sb.AppendLine("64-bit process: " + Environment.Is64BitProcess);
            sb.AppendLine(".NET: " + Environment.Version);
            sb.AppendLine();

            sb.AppendLine("[Microphone]");
            sb.AppendLine("Device count: " + WaveInEvent.DeviceCount);
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var caps = WaveInEvent.GetCapabilities(i);
                sb.AppendLine("- " + i + ": " + caps.ProductName + " / Channels: " + caps.Channels);
            }
            sb.AppendLine();

            sb.AppendLine("[Whisper model]");
            sb.AppendLine("Model path: " + modelPath);
            sb.AppendLine("Model exists: " + File.Exists(modelPath));
            if (File.Exists(modelPath)) sb.AppendLine("Model size: " + new FileInfo(modelPath).Length + " bytes");
            sb.AppendLine();

            sb.AppendLine("[Native runtime]");
            sb.AppendLine("Runtime native folder: " + runtimeNativeDir);
            sb.AppendLine("Runtime native folder exists: " + Directory.Exists(runtimeNativeDir));
            var candidates = new[]
            {
                Path.Combine(baseDir, "whisper.dll"),
                Path.Combine(baseDir, "ggml.dll"),
                Path.Combine(runtimeNativeDir, "whisper.dll"),
                Path.Combine(runtimeNativeDir, "ggml.dll")
            };
            foreach (var file in candidates)
            {
                sb.AppendLine(file + " => " + File.Exists(file));
            }
            if (Directory.Exists(runtimeNativeDir))
            {
                sb.AppendLine();
                sb.AppendLine("Files in native folder:");
                foreach (var file in Directory.GetFiles(runtimeNativeDir))
                {
                    sb.AppendLine("- " + Path.GetFileName(file) + " / " + new FileInfo(file).Length + " bytes");
                }
            }

            File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);

            var summary =
                "환경진단이 완료되었습니다.\nDiagnostics completed.\n\n" +
                "마이크 개수 / Microphones: " + WaveInEvent.DeviceCount + "\n" +
                "모델 파일 / Model: " + (File.Exists(modelPath) ? "OK" : "없음 / Missing") + "\n" +
                "Whisper Runtime 폴더 / Runtime folder: " + (Directory.Exists(runtimeNativeDir) ? "OK" : "없음 / Missing") + "\n\n" +
                "진단 로그 / Log:\n" + logPath;

            MessageBox.Show(summary, "환경진단 / Diagnostics");
            SetStatus("환경진단 완료 / Diagnostics completed");
        }
        catch (Exception ex)
        {
            var logPath = SaveErrorLog("Environment diagnostics failed", ex);
            MessageBox.Show("환경진단 중 오류가 발생했습니다.\nDiagnostics failed.\n\n" + ex.Message + "\n\nLog:\n" + logPath, "환경진단 오류 / Diagnostics Error");
        }
    }

    private void CheckMic()
    {
        try
        {
            var count = WaveInEvent.DeviceCount;
            if (count <= 0)
            {
                MessageBox.Show("사용 가능한 마이크가 없습니다.\nNo microphone device was found.", "마이크 확인 / Check Mic");
                return;
            }

            var info = "";
            for (int i = 0; i < count; i++)
            {
                var caps = WaveInEvent.GetCapabilities(i);
                info += "- " + caps.ProductName + "\n";
            }

            MessageBox.Show("사용 가능한 마이크 / Available microphones:\n\n" + info +
                            "\n이 버전은 Windows 음성 인식 엔진이 아니라 Whisper 방식을 사용합니다.\n" +
                            "This version uses Whisper, not Windows speech recognition.",
                            "마이크 확인 / Check Mic");
        }
        catch (Exception ex)
        {
            MessageBox.Show("마이크 정보를 확인할 수 없습니다.\nCould not check microphone information.\n\n" + ex.Message, "마이크 확인 오류 / Check Mic Error");
        }
    }

    private async Task TranscribeWavAsync(string wavPath)
    {
        try
        {
            var modelPath = await EnsureWhisperModelAsync();
            SetStatus("Whisper 음성 변환 중... / Transcribing with Whisper...");

            var sb = new StringBuilder();
            EnsureLocalWhisperRuntimeLooksReady();
            using var whisperFactory = WhisperFactory.FromPath(modelPath);
            using var processor = whisperFactory.CreateBuilder()
                .WithLanguage("ko")
                .Build();

            await using var fileStream = File.OpenRead(wavPath);
            await foreach (var result in processor.ProcessAsync(fileStream))
            {
                sb.Append(result.Text);
            }

            var text = sb.ToString().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                SetStatus("음성을 텍스트로 변환하지 못했습니다. / No speech detected.");
                MessageBox.Show("음성을 텍스트로 변환하지 못했습니다.\nNo speech was detected.", "Whisper");
                return;
            }

            koreanBox.AppendText((koreanBox.Text.Trim().Length > 0 ? Environment.NewLine : "") + text);
            SetStatus("Whisper 변환 완료 / Whisper transcription complete");
            await TranslateAsync();
        }
        catch (Exception ex)
        {
            SetStatus("Whisper 오류 / Whisper error");

            var logPath = SaveErrorLog("Whisper transcription failed", ex);

            MessageBox.Show(
                "Whisper 음성 변환 중 오류가 발생했습니다.\n" +
                "Whisper transcription failed.\n\n" +
                "오류 내용 / Error:\n" + ex.Message + "\n\n" +
                "상세 로그 저장 위치 / Error log:\n" + logPath + "\n\n" +
                "이 내용을 복사해서 저에게 보내주세요.",
                "Whisper Error");
        }
    }

    private void EnsureLocalWhisperRuntimeLooksReady()
    {
        var baseDir = AppContext.BaseDirectory;
        var runtimeNativeDir = Path.Combine(baseDir, "runtimes", "win-x64", "native");
        var likelyFiles = new[]
        {
            Path.Combine(baseDir, "whisper.dll"),
            Path.Combine(baseDir, "ggml.dll"),
            Path.Combine(runtimeNativeDir, "whisper.dll"),
            Path.Combine(runtimeNativeDir, "ggml.dll")
        };

        if (Directory.Exists(runtimeNativeDir) || likelyFiles.Any(File.Exists))
            return;

        throw new FileNotFoundException(
            @"로컬 Whisper Runtime DLL을 찾지 못했습니다. GitHub Actions가 SingleFile=false로 빌드되어야 하며, publish 폴더의 runtimes\win-x64\native 폴더가 EXE와 함께 있어야 합니다. / Local Whisper native DLLs were not found. Build with PublishSingleFile=false and keep the runtimes\win-x64\native folder next to the EXE.");
    }

    private async Task<string> EnsureWhisperModelAsync()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LivingKoreaStudio", "models");
        Directory.CreateDirectory(appData);
        var modelPath = Path.Combine(appData, "ggml-tiny.bin");

        if (File.Exists(modelPath) && new FileInfo(modelPath).Length > 50_000_000)
            return modelPath;

        if (File.Exists(modelPath))
        {
            try { File.Delete(modelPath); } catch { }
        }

        SetStatus("Whisper 모델 다운로드 중... 첫 실행은 시간이 걸립니다. / Downloading Whisper model...");
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(20);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LivingKoreaStudio/1.1.2");

        using var response = await client.GetAsync(ModelUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync();
        await using var file = File.Create(modelPath);
        await source.CopyToAsync(file);

        var size = new FileInfo(modelPath).Length;
        if (size < 50_000_000)
        {
            try { File.Delete(modelPath); } catch { }
            throw new Exception("Whisper model download failed or file is too small. Downloaded size: " + size + " bytes");
        }

        return modelPath;
    }

    private string SaveErrorLog(string title, Exception ex)
    {
        var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LivingKoreaStudio", "logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "error_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");

        var text =
            title + Environment.NewLine +
            "Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
            "Current WAV: " + (currentWavPath ?? "(none)") + Environment.NewLine +
            "WAV exists: " + (!string.IsNullOrWhiteSpace(currentWavPath) && File.Exists(currentWavPath)) + Environment.NewLine +
            "Message: " + ex.Message + Environment.NewLine +
            "Details:" + Environment.NewLine +
            ex.ToString();

        File.WriteAllText(logPath, text, Encoding.UTF8);
        return logPath;
    }

    private async Task TranslateAsync()
    {
        var ko = koreanBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(ko)) return;

        try
        {
            SetStatus("번역 중... / Translating...");
            var translated = await TranslateWithMyMemoryAsync(ko);

            if (styleBox.Text.StartsWith("유튜브"))
            {
                translated = "Hello everyone, welcome to Living Korea!\r\n\r\n" +
                             translated +
                             "\r\n\r\nIf this was helpful, please like and subscribe for more real-life tips about Korea.";
            }
            else if (styleBox.Text.StartsWith("정중한"))
            {
                translated = "Hello. Today, I would like to explain this topic clearly and politely.\r\n\r\n" + translated;
            }

            englishBox.Text = translated;
            SetStatus("번역 완료 / Translation complete");
        }
        catch (Exception ex)
        {
            SetStatus("번역 오류 / Translation error");
            MessageBox.Show("번역 중 오류가 발생했습니다.\nTranslation failed.\n\n" + ex.Message, "번역 오류 / Translation Error");
        }
    }

    private static async Task<string> TranslateWithMyMemoryAsync(string text)
    {
        using var client = new HttpClient();
        var url = "https://api.mymemory.translated.net/get?q=" + Uri.EscapeDataString(text) + "&langpair=ko|en";
        var json = await client.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("responseData").GetProperty("translatedText").GetString() ?? "";
    }

    private void AddToMemo()
    {
        var ko = koreanBox.Text.Trim();
        var en = englishBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(ko) || string.IsNullOrWhiteSpace(en))
        {
            MessageBox.Show("한국어와 영어 번역 결과가 모두 있어야 합니다.\nKorean and English text are required.", "확인 / Check");
            return;
        }

        var project = string.IsNullOrWhiteSpace(projectBox.Text) ? "Living Korea" : projectBox.Text.Trim();
        var block =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm}]  Project: {project}\r\n" +
            $"한국어 / Korean:\r\n{ko}\r\n\r\n" +
            $"영어 / English:\r\n{en}\r\n\r\n" +
            "--------------------------------------------------\r\n\r\n";

        memoBox.Text = block + memoBox.Text;
        SetStatus("메모장에 추가했습니다. / Added to notes.");
    }

    private void CopyBoth()
    {
        Clipboard.SetText($"한국어 / Korean:\r\n{koreanBox.Text.Trim()}\r\n\r\n영어 / English:\r\n{englishBox.Text.Trim()}");
        SetStatus("복사 완료 / Copied");
    }

    private void SaveTxt()
    {
        var project = string.IsNullOrWhiteSpace(projectBox.Text) ? "Living_Korea" : projectBox.Text.Trim().Replace(" ", "_");
        using var dialog = new SaveFileDialog
        {
            Filter = "Text File (*.txt)|*.txt",
            FileName = $"{project}_Translation_{DateTime.Now:yyyyMMdd}.txt"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var content = string.IsNullOrWhiteSpace(memoBox.Text)
                ? $"한국어 / Korean:\r\n{koreanBox.Text.Trim()}\r\n\r\n영어 / English:\r\n{englishBox.Text.Trim()}"
                : memoBox.Text;

            System.IO.File.WriteAllText(dialog.FileName, content, Encoding.UTF8);
            SetStatus("TXT 저장 완료 / TXT saved");
        }
    }
}
