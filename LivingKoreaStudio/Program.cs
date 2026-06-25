using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Speech.Recognition;

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
    private readonly TextBox koreanBox = new();
    private readonly TextBox englishBox = new();
    private readonly TextBox memoBox = new();
    private readonly Label statusLabel = new();
    private readonly ComboBox styleBox = new();
    private readonly Button micButton = new();
    private readonly Button micCheckButton = new();

    private SpeechRecognitionEngine? recognizer;
    private bool isListening = false;

    public MainForm()
    {
        Text = "Living Korea Studio Pro";
        Width = 1200;
        Height = 840;
        MinimumSize = new System.Drawing.Size(1000, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BuildUI();
        InitSpeech();
    }

    private void BuildUI()
    {
        Font = new System.Drawing.Font("맑은 고딕", 10);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.FromArgb(11, 85, 217) };
        header.Controls.Add(new Label
        {
            Text = "Living Korea Studio Pro",
            ForeColor = System.Drawing.Color.White,
            Font = new System.Drawing.Font("Arial", 22, System.Drawing.FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 42,
            Padding = new Padding(12, 8, 0, 0)
        });
        header.Controls.Add(new Label
        {
            Text = "한국어 음성 입력 · 영어 번역 · 유튜브 대본 메모장  /  Korean Voice Input · English Translation · YouTube Script Notes",
            ForeColor = System.Drawing.Color.White,
            Font = new System.Drawing.Font("맑은 고딕", 10),
            Dock = DockStyle.Bottom,
            Height = 34,
            Padding = new Padding(14, 0, 0, 9)
        });
        root.Controls.Add(header, 0, 0);

        var top = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        top.Controls.Add(new Label
        {
            Text = "번역 스타일 / Style",
            Width = 140,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        });

        styleBox.Width = 230;
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

        statusLabel.Text = "대기 중 / Ready";
        statusLabel.AutoSize = true;
        statusLabel.Padding = new Padding(20, 9, 0, 0);
        top.Controls.Add(statusLabel);
        root.Controls.Add(top, 0, 1);

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 575 };

        koreanBox.Multiline = true;
        koreanBox.ScrollBars = ScrollBars.Vertical;
        koreanBox.Font = new System.Drawing.Font("맑은 고딕", 12);
        koreanBox.Dock = DockStyle.Fill;
        koreanBox.PlaceholderText = "여기에 한국어를 입력하거나 마이크 시작을 누르고 말하세요.\r\nType Korean here or press Start Mic and speak.";

        englishBox.Multiline = true;
        englishBox.ScrollBars = ScrollBars.Vertical;
        englishBox.Font = new System.Drawing.Font("Arial", 12);
        englishBox.Dock = DockStyle.Fill;
        englishBox.ForeColor = System.Drawing.Color.FromArgb(11, 47, 128);
        englishBox.PlaceholderText = "영어 번역 결과가 여기에 표시됩니다.\r\nEnglish translation will appear here.";

        split.Panel1.Controls.Add(WrapWithLabel("한국어 입력 / Korean Input", koreanBox));
        split.Panel2.Controls.Add(WrapWithLabel("영어 번역 결과 / English Translation", englishBox));
        root.Controls.Add(split, 0, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };

        micButton.Text = "마이크 시작 / Start Mic";
        micButton.Width = 155;
        micButton.Click += (_, _) => ToggleMic();

        micCheckButton.Text = "마이크 확인 / Check Mic";
        micCheckButton.Width = 160;
        micCheckButton.Click += (_, _) => ShowSpeechInfo();

        var translateButton = new Button { Text = "번역하기 / Translate", Width = 145 };
        translateButton.Click += async (_, _) => await TranslateAsync();

        var addMemoButton = new Button { Text = "메모 추가 / Add Note", Width = 145 };
        addMemoButton.Click += (_, _) => AddToMemo();

        var copyButton = new Button { Text = "둘 다 복사 / Copy Both", Width = 155 };
        copyButton.Click += (_, _) => CopyBoth();

        var saveButton = new Button { Text = "TXT 저장 / Save", Width = 125 };
        saveButton.Click += (_, _) => SaveTxt();

        var clearButton = new Button { Text = "비우기 / Clear", Width = 115 };
        clearButton.Click += (_, _) => { koreanBox.Clear(); englishBox.Clear(); SetStatus("입력창을 비웠습니다. / Cleared"); };

        buttons.Controls.AddRange(new Control[] { micButton, micCheckButton, translateButton, addMemoButton, copyButton, saveButton, clearButton });
        root.Controls.Add(buttons, 0, 3);

        memoBox.Multiline = true;
        memoBox.ScrollBars = ScrollBars.Vertical;
        memoBox.Font = new System.Drawing.Font("맑은 고딕", 10);
        memoBox.Dock = DockStyle.Fill;
        memoBox.PlaceholderText = "저장한 번역 메모가 여기에 쌓입니다.\r\nSaved translation notes will appear here.";
        root.Controls.Add(WrapWithLabel("번역 메모장 / Translation Notes", memoBox), 0, 4);
    }

    private static Control WrapWithLabel(string label, Control inner)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            Font = new System.Drawing.Font("맑은 고딕", 11, System.Drawing.FontStyle.Bold),
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        }, 0, 0);
        panel.Controls.Add(inner, 0, 1);
        return panel;
    }

    private void InitSpeech()
    {
        try
        {
            var installed = SpeechRecognitionEngine.InstalledRecognizers();
            var koreanRecognizer = installed.FirstOrDefault(r =>
                r.Culture.Name.Equals("ko-KR", StringComparison.OrdinalIgnoreCase) ||
                r.Culture.TwoLetterISOLanguageName.Equals("ko", StringComparison.OrdinalIgnoreCase));

            if (koreanRecognizer == null)
            {
                micButton.Enabled = false;
                micButton.Text = "마이크 사용 불가 / Mic Unavailable";
                SetStatus("한국어 음성 인식 엔진이 없습니다. / Korean speech engine not found.");
                return;
            }

            recognizer = new SpeechRecognitionEngine(koreanRecognizer);
            recognizer.SetInputToDefaultAudioDevice();
            recognizer.LoadGrammar(new DictationGrammar());

            recognizer.SpeechRecognized += async (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Result.Text))
                {
                    koreanBox.AppendText((koreanBox.Text.Trim().Length > 0 ? Environment.NewLine : "") + e.Result.Text);
                    SetStatus("음성 입력 완료 / Voice captured: " + e.Result.Text);
                    await TranslateAsync();
                }
                else
                {
                    SetStatus("음성을 인식하지 못했습니다. / Speech not recognized.");
                }
            };

            recognizer.SpeechRecognitionRejected += (_, _) =>
            {
                SetStatus("음성을 인식하지 못했습니다. / Speech rejected. Try speaking more clearly.");
            };

            recognizer.RecognizeCompleted += (_, e) =>
            {
                if (e.Error != null)
                {
                    SetStatus("마이크 오류 / Mic error: " + e.Error.Message);
                    return;
                }

                if (isListening)
                {
                    try
                    {
                        SetStatus("듣는 중... / Listening...");
                        recognizer?.RecognizeAsync(RecognizeMode.Single);
                    }
                    catch
                    {
                        SetStatus("마이크 재시작 오류 / Mic restart error.");
                    }
                }
            };

            SetStatus("마이크 준비 완료 / Mic ready");
        }
        catch (Exception ex)
        {
            micButton.Enabled = false;
            micButton.Text = "마이크 오류 / Mic Error";
            SetStatus("마이크 초기화 실패 / Mic initialization failed");
            MessageBox.Show("마이크 초기화에 실패했습니다.\nMic initialization failed.\n\n" + ex.Message, "마이크 오류 / Mic Error");
        }
    }

    private void ShowSpeechInfo()
    {
        try
        {
            var installed = SpeechRecognitionEngine.InstalledRecognizers();

            if (installed.Count == 0)
            {
                MessageBox.Show(
                    "설치된 Windows 음성 인식 엔진이 없습니다.\n\n" +
                    "No Windows speech recognition engine is installed.\n\n" +
                    "Windows 설정 → 시간 및 언어 → 언어 및 지역 → 한국어 → 언어 옵션 → 음성 인식 설치\n" +
                    "Windows Settings → Time & Language → Language & Region → Korean → Language Options → Install Speech",
                    "마이크 확인 / Check Mic");
                return;
            }

            var info = string.Join("\n", installed.Select(r => "- " + r.Culture.Name + " / " + r.Description));
            MessageBox.Show(
                "설치된 음성 인식 엔진 / Installed speech engines:\n\n" + info + "\n\n" +
                "한국어 인식에는 ko-KR 항목이 필요합니다.\nKorean recognition requires ko-KR.",
                "마이크 확인 / Check Mic");
        }
        catch (Exception ex)
        {
            MessageBox.Show("음성 인식 정보를 확인할 수 없습니다.\nCould not check speech information.\n\n" + ex.Message, "마이크 확인 오류 / Check Mic Error");
        }
    }

    private void ToggleMic()
    {
        if (recognizer == null)
        {
            ShowSpeechInfo();
            return;
        }

        try
        {
            if (!isListening)
            {
                isListening = true;
                micButton.Text = "마이크 중지 / Stop Mic";
                SetStatus("듣는 중... 한국어로 말해보세요. / Listening... Please speak Korean.");
                recognizer.RecognizeAsync(RecognizeMode.Single);
            }
            else
            {
                isListening = false;
                micButton.Text = "마이크 시작 / Start Mic";
                recognizer.RecognizeAsyncCancel();
                SetStatus("마이크 중지됨 / Mic stopped");
            }
        }
        catch (Exception ex)
        {
            SetStatus("마이크 오류 / Mic error");
            MessageBox.Show(ex.Message, "마이크 오류 / Mic Error");
        }
    }

    private void SetStatus(string text)
    {
        statusLabel.Text = text;
        statusLabel.Refresh();
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
                translated =
                    "Hello everyone, welcome to Living Korea!\r\n\r\n" +
                    translated +
                    "\r\n\r\nIf this was helpful, please like and subscribe for more real-life tips about Korea.";
            }
            else if (styleBox.Text.StartsWith("정중한"))
            {
                translated =
                    "Hello. Today, I would like to explain this topic clearly and politely.\r\n\r\n" +
                    translated;
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
        var url = "https://api.mymemory.translated.net/get?q=" +
                  Uri.EscapeDataString(text) +
                  "&langpair=ko|en";

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

        var block =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm}]\r\n" +
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
        using var dialog = new SaveFileDialog
        {
            Filter = "Text File (*.txt)|*.txt",
            FileName = $"Living_Korea_Translation_{DateTime.Now:yyyyMMdd}.txt"
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
