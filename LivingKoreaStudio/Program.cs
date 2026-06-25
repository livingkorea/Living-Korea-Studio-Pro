using System;
using System.Globalization;
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
    private SpeechRecognitionEngine? recognizer;
    private bool isListening = false;

    public MainForm()
    {
        Text = "Living Korea Studio";
        Width = 1180;
        Height = 820;
        StartPosition = FormStartPosition.CenterScreen;
        BuildUI();
        InitSpeech();
    }

    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.FromArgb(11, 85, 217) };
        header.Controls.Add(new Label
        {
            Text = "Living Korea Studio",
            ForeColor = System.Drawing.Color.White,
            Font = new System.Drawing.Font("Arial", 22, System.Drawing.FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 38,
            Padding = new Padding(12, 8, 0, 0)
        });
        header.Controls.Add(new Label
        {
            Text = "Voice input + Korean to English translation note for your YouTube channel",
            ForeColor = System.Drawing.Color.White,
            Font = new System.Drawing.Font("Arial", 10),
            Dock = DockStyle.Bottom,
            Height = 26,
            Padding = new Padding(14, 0, 0, 7)
        });
        root.Controls.Add(header, 0, 0);

        var top = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        top.Controls.Add(new Label { Text = "Translation Style", Width = 120, TextAlign = System.Drawing.ContentAlignment.MiddleLeft });
        styleBox.Width = 190;
        styleBox.DropDownStyle = ComboBoxStyle.DropDownList;
        styleBox.Items.AddRange(new object[] { "YouTube Script", "Natural English", "Simple English", "Formal" });
        styleBox.SelectedIndex = 0;
        top.Controls.Add(styleBox);

        statusLabel.Text = "Ready";
        statusLabel.AutoSize = true;
        statusLabel.Padding = new Padding(20, 9, 0, 0);
        top.Controls.Add(statusLabel);
        root.Controls.Add(top, 0, 1);

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 560 };

        koreanBox.Multiline = true;
        koreanBox.ScrollBars = ScrollBars.Vertical;
        koreanBox.Font = new System.Drawing.Font("Malgun Gothic", 12);
        koreanBox.Dock = DockStyle.Fill;

        englishBox.Multiline = true;
        englishBox.ScrollBars = ScrollBars.Vertical;
        englishBox.Font = new System.Drawing.Font("Arial", 12);
        englishBox.Dock = DockStyle.Fill;
        englishBox.ForeColor = System.Drawing.Color.FromArgb(11, 47, 128);

        split.Panel1.Controls.Add(WrapWithLabel("Korean Input / Voice Input", koreanBox));
        split.Panel2.Controls.Add(WrapWithLabel("English Translation", englishBox));
        root.Controls.Add(split, 0, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };

        micButton.Text = "Mic Start";
        micButton.Width = 120;
        micButton.Click += (_, _) => ToggleMic();

        var translateButton = new Button { Text = "Translate", Width = 120 };
        translateButton.Click += async (_, _) => await TranslateAsync();

        var addMemoButton = new Button { Text = "Add to Note", Width = 120 };
        addMemoButton.Click += (_, _) => AddToMemo();

        var copyButton = new Button { Text = "Copy Both", Width = 120 };
        copyButton.Click += (_, _) => CopyBoth();

        var saveButton = new Button { Text = "Save TXT", Width = 120 };
        saveButton.Click += (_, _) => SaveTxt();

        var clearButton = new Button { Text = "Clear", Width = 120 };
        clearButton.Click += (_, _) => { koreanBox.Clear(); englishBox.Clear(); SetStatus("Cleared"); };

        buttons.Controls.AddRange(new Control[] { micButton, translateButton, addMemoButton, copyButton, saveButton, clearButton });
        root.Controls.Add(buttons, 0, 3);

        memoBox.Multiline = true;
        memoBox.ScrollBars = ScrollBars.Vertical;
        memoBox.Font = new System.Drawing.Font("Malgun Gothic", 10);
        memoBox.Dock = DockStyle.Fill;
        root.Controls.Add(WrapWithLabel("Translation Note", memoBox), 0, 4);
    }

    private static Control WrapWithLabel(string label, Control inner)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            Font = new System.Drawing.Font("Malgun Gothic", 11, System.Drawing.FontStyle.Bold),
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
            recognizer = new SpeechRecognitionEngine(new CultureInfo("ko-KR"));
            recognizer.SetInputToDefaultAudioDevice();
            recognizer.LoadGrammar(new DictationGrammar());
            recognizer.SpeechRecognized += async (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Result.Text))
                {
                    koreanBox.AppendText((koreanBox.Text.Trim().Length > 0 ? Environment.NewLine : "") + e.Result.Text);
                    SetStatus("Voice input added");
                    await TranslateAsync();
                }
            };
            recognizer.RecognizeCompleted += (_, _) =>
            {
                if (isListening)
                {
                    try { recognizer?.RecognizeAsync(RecognizeMode.Single); } catch { }
                }
            };
        }
        catch
        {
            micButton.Enabled = false;
            micButton.Text = "Mic unavailable";
            SetStatus("Korean speech recognition is not available on this Windows PC.");
        }
    }

    private void ToggleMic()
    {
        if (recognizer == null) return;

        try
        {
            if (!isListening)
            {
                isListening = true;
                micButton.Text = "Mic Stop";
                SetStatus("Listening...");
                recognizer.RecognizeAsync(RecognizeMode.Single);
            }
            else
            {
                isListening = false;
                micButton.Text = "Mic Start";
                recognizer.RecognizeAsyncCancel();
                SetStatus("Mic stopped");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Mic Error");
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
            SetStatus("Translating...");
            var translated = await TranslateWithMyMemoryAsync(ko);

            if (styleBox.Text == "YouTube Script")
            {
                translated = "Hello everyone, welcome to Living Korea!\r\n\r\n" +
                             translated +
                             "\r\n\r\nIf this was helpful, please like and subscribe for more real-life tips about Korea.";
            }
            else if (styleBox.Text == "Formal")
            {
                translated = "Hello. Today, I would like to explain this topic clearly and politely.\r\n\r\n" + translated;
            }

            englishBox.Text = translated;
            SetStatus("Done");
        }
        catch (Exception ex)
        {
            SetStatus("Translation Error");
            MessageBox.Show(ex.Message, "Translation Error");
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
            MessageBox.Show("Korean and English text are required.");
            return;
        }

        var block =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm}]\r\n" +
            $"Korean:\r\n{ko}\r\n\r\n" +
            $"English:\r\n{en}\r\n\r\n" +
            "--------------------------------------------------\r\n\r\n";

        memoBox.Text = block + memoBox.Text;
        SetStatus("Added to note");
    }

    private void CopyBoth()
    {
        Clipboard.SetText($"Korean:\r\n{koreanBox.Text.Trim()}\r\n\r\nEnglish:\r\n{englishBox.Text.Trim()}");
        SetStatus("Copied");
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
                ? $"Korean:\r\n{koreanBox.Text.Trim()}\r\n\r\nEnglish:\r\n{englishBox.Text.Trim()}"
                : memoBox.Text;

            System.IO.File.WriteAllText(dialog.FileName, content, Encoding.UTF8);
            SetStatus("Saved");
        }
    }
}
