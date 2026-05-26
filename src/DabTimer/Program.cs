using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DabTimer
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TimerForm());
        }
    }

    internal sealed class TimerForm : Form
    {
        private const int TotalSeconds = 90;
        private const int HeatFinishedSeconds = 30;
        private const int HeatFinishedRemaining = TotalSeconds - HeatFinishedSeconds;
        private const int GetReadyRemaining = 20;
        private const int DropDabSeconds = 10;
        private const int DabTimePostSeconds = 20;

        private readonly string settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DabTimer",
            "settings.txt");
        private readonly string soundCacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DabTimer",
            "sounds");

        private Color gradientStart = ColorTranslator.FromHtml("#171B22");
        private Color gradientEnd = ColorTranslator.FromHtml("#155BBA");
        private Color renderedGradientStart = ColorTranslator.FromHtml("#171B22");
        private Color renderedGradientEnd = ColorTranslator.FromHtml("#155BBA");
        private readonly Color defaultForeground = Color.White;
        private readonly Color heatingForeground = ColorTranslator.FromHtml("#ff6118");
        private readonly Color coolingForeground = ColorTranslator.FromHtml("#90dfff");
        private readonly Color dropForeground = ColorTranslator.FromHtml("#ffa6c9");
        private readonly Color dabbingForeground = ColorTranslator.FromHtml("#90dfff");
        private readonly Color buttonBackground = ColorTranslator.FromHtml("#071010");
        private readonly Color buttonActiveBackground = ColorTranslator.FromHtml("#174746");

        private readonly GlowLabel phaseLabel;
        private readonly GlowLabel timeLabel;
        private readonly Button startButton;
        private readonly Button stopButton;
        private readonly Button settingsButton;
        private readonly Button closeButton;
        private readonly SpeakerButton speakerButton;
        private readonly Panel settingsPanel;
        private readonly Panel volumePanel;
        private readonly ColorSlider startRedSlider;
        private readonly ColorSlider startGreenSlider;
        private readonly ColorSlider startBlueSlider;
        private readonly ColorSlider endRedSlider;
        private readonly ColorSlider endGreenSlider;
        private readonly ColorSlider endBlueSlider;
        private readonly TextBox startHexBox;
        private readonly TextBox endHexBox;
        private readonly Panel startPreview;
        private readonly Panel endPreview;
        private readonly VolumeSlider volumeSlider;
        private readonly Label volumeLabel;
        private readonly Timer startDelayTimer;
        private readonly Timer countdownTimer;
        private readonly Timer postPhaseTimer;
        private readonly Timer gradientAnimationTimer;
        private readonly Timer phaseAnimationTimer;

        private int remaining = TotalSeconds;
        private int postPhaseRemaining;
        private int soundVolume = 30;
        private string pendingPhaseText = "";
        private Color pendingPhaseColor = Color.White;
        private bool phaseFadingOut;
        private bool running;
        private bool heatSoundPlayed;
        private bool updatingColorFields;
        private string postPhaseName = "";

        public TimerForm()
        {
            LoadSettings();
            renderedGradientStart = gradientStart;
            renderedGradientEnd = gradientEnd;

            Text = "DAB TIME";
            ClientSize = new Size(800, 540);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            Icon = LoadIconResource();

            phaseLabel = new GlowLabel();
            phaseLabel.AutoSize = false;
            phaseLabel.BackColor = Color.Transparent;
            phaseLabel.ForeColor = defaultForeground;
            phaseLabel.Font = CreateDisplayFont(38f);
            phaseLabel.Text = "DAB TIME";
            phaseLabel.TextAlign = ContentAlignment.MiddleCenter;
            phaseLabel.SetBounds(0, 18, ClientSize.Width, 76);
            phaseLabel.MouseDown += DragMouseDown;
            Controls.Add(phaseLabel);

            timeLabel = new GlowLabel();
            timeLabel.AutoSize = false;
            timeLabel.BackColor = Color.Transparent;
            timeLabel.ForeColor = defaultForeground;
            timeLabel.Font = CreateDisplayFont(106f);
            timeLabel.Text = FormatTime(remaining);
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;
            timeLabel.SetBounds(0, 78, ClientSize.Width, 220);
            timeLabel.MouseDown += DragMouseDown;
            Controls.Add(timeLabel);

            startButton = CreateButton("Start");
            startButton.SetBounds(210, 326, 170, 70);
            startButton.Click += StartClicked;
            Controls.Add(startButton);

            stopButton = CreateButton("Stop");
            stopButton.SetBounds(420, 326, 170, 70);
            stopButton.Enabled = false;
            stopButton.Click += StopClicked;
            Controls.Add(stopButton);

            closeButton = CreateCloseButton();
            closeButton.SetBounds(ClientSize.Width - 38, 12, 24, 24);
            closeButton.Click += CloseClicked;
            Controls.Add(closeButton);

            settingsButton = CreateIconButton("+");
            settingsButton.SetBounds(16, ClientSize.Height - 48, 34, 34);
            settingsButton.Click += ToggleSettings;
            Controls.Add(settingsButton);

            speakerButton = new SpeakerButton();
            speakerButton.SetBounds(ClientSize.Width - 50, ClientSize.Height - 48, 34, 34);
            speakerButton.Click += ToggleVolume;
            Controls.Add(speakerButton);

            settingsPanel = new Panel();
            settingsPanel.SetBounds(16, ClientSize.Height - 346, 438, 286);
            settingsPanel.BackColor = Color.Transparent;
            settingsPanel.Visible = false;
            Controls.Add(settingsPanel);
            settingsPanel.BringToFront();
            settingsButton.BringToFront();
            speakerButton.BringToFront();
            closeButton.BringToFront();

            volumePanel = new Panel();
            volumePanel.SetBounds(ClientSize.Width - 78, ClientSize.Height - 234, 62, 174);
            volumePanel.BackColor = Color.Transparent;
            volumePanel.Visible = false;
            Controls.Add(volumePanel);
            volumePanel.BringToFront();
            speakerButton.BringToFront();
            closeButton.BringToFront();

            volumeSlider = new VolumeSlider();
            volumeSlider.SetBounds(18, 10, 26, 118);
            volumeSlider.Value = soundVolume;
            volumeSlider.ValueChanged += VolumeSliderChanged;
            volumePanel.Controls.Add(volumeSlider);

            volumeLabel = new Label();
            volumeLabel.SetBounds(0, 136, volumePanel.Width, 24);
            volumeLabel.BackColor = Color.Transparent;
            volumeLabel.ForeColor = Color.White;
            volumeLabel.Font = new Font("Arial Black", 8f, FontStyle.Bold, GraphicsUnit.Point);
            volumeLabel.TextAlign = ContentAlignment.MiddleCenter;
            volumePanel.Controls.Add(volumeLabel);
            UpdateVolumeLabel();

            startPreview = CreatePreview(24, 20);
            settingsPanel.Controls.Add(startPreview);
            endPreview = CreatePreview(226, 20);
            settingsPanel.Controls.Add(endPreview);

            startRedSlider = CreateSlider(24, 62, gradientStart.R);
            startGreenSlider = CreateSlider(24, 108, gradientStart.G);
            startBlueSlider = CreateSlider(24, 154, gradientStart.B);
            endRedSlider = CreateSlider(226, 62, gradientEnd.R);
            endGreenSlider = CreateSlider(226, 108, gradientEnd.G);
            endBlueSlider = CreateSlider(226, 154, gradientEnd.B);

            settingsPanel.Controls.Add(startRedSlider);
            settingsPanel.Controls.Add(startGreenSlider);
            settingsPanel.Controls.Add(startBlueSlider);
            settingsPanel.Controls.Add(endRedSlider);
            settingsPanel.Controls.Add(endGreenSlider);
            settingsPanel.Controls.Add(endBlueSlider);

            startHexBox = CreateHexBox(24, 222, ColorToHex(gradientStart));
            endHexBox = CreateHexBox(226, 222, ColorToHex(gradientEnd));
            settingsPanel.Controls.Add(startHexBox);
            settingsPanel.Controls.Add(endHexBox);
            AddSliderLabels();

            startDelayTimer = new Timer();
            startDelayTimer.Interval = 1000;
            startDelayTimer.Tick += BeginHeating;

            countdownTimer = new Timer();
            countdownTimer.Interval = 1000;
            countdownTimer.Tick += CountdownTick;

            postPhaseTimer = new Timer();
            postPhaseTimer.Interval = 1000;
            postPhaseTimer.Tick += PostPhaseTick;

            gradientAnimationTimer = new Timer();
            gradientAnimationTimer.Interval = 16;
            gradientAnimationTimer.Tick += GradientAnimationTick;

            phaseAnimationTimer = new Timer();
            phaseAnimationTimer.Interval = 16;
            phaseAnimationTimer.Tick += PhaseAnimationTick;
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int mciSendString(string command, string returnValue, int returnLength, IntPtr winHandle);

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (LinearGradientBrush brush = new LinearGradientBrush(ClientRectangle, renderedGradientStart, renderedGradientEnd, 135f))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            using (SolidBrush vignette = new SolidBrush(Color.FromArgb(82, 0, 0, 0)))
            {
                e.Graphics.FillEllipse(vignette, -130, -110, ClientSize.Width + 260, ClientSize.Height + 220);
            }

            using (Pen outline = new Pen(Color.FromArgb(130, 255, 255, 255), 1f))
            {
                e.Graphics.DrawRectangle(outline, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            BeginWindowDrag(e);
        }

        private void BeginWindowDrag(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            ReleaseCapture();
            SendMessage(Handle, 0xA1, 0x2, 0);
        }

        private void DragMouseDown(object sender, MouseEventArgs e)
        {
            BeginWindowDrag(e);
        }

        private Button CreateButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Font = CreateDisplayFont(21f);
            button.BackColor = buttonBackground;
            button.ForeColor = defaultForeground;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = buttonActiveBackground;
            button.FlatAppearance.MouseDownBackColor = buttonActiveBackground;
            button.UseVisualStyleBackColor = false;
            return button;
        }

        private Button CreateIconButton(string text)
        {
            Button button = CreateButton(text);
            button.Font = CreateDisplayFont(22f);
            button.TextAlign = ContentAlignment.MiddleCenter;
            return button;
        }

        private Button CreateCloseButton()
        {
            Button button = new Button();
            button.Text = "X";
            button.Font = new Font("Arial", 9f, FontStyle.Bold, GraphicsUnit.Point);
            button.BackColor = Color.Transparent;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(90, 255, 255, 255);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(130, 255, 255, 255);
            button.UseVisualStyleBackColor = false;
            button.TextAlign = ContentAlignment.MiddleCenter;
            return button;
        }

        private Panel CreatePreview(int x, int y)
        {
            Panel panel = new Panel();
            panel.SetBounds(x, y, 170, 22);
            panel.BackColor = Color.White;
            return panel;
        }

        private ColorSlider CreateSlider(int x, int y, int value)
        {
            ColorSlider slider = new ColorSlider();
            slider.SetBounds(x, y, 170, 34);
            slider.Value = value;
            slider.ValueChanged += ColorSliderChanged;
            return slider;
        }

        private TextBox CreateHexBox(int x, int y, string value)
        {
            TextBox box = new TextBox();
            box.SetBounds(x, y, 170, 28);
            box.BorderStyle = BorderStyle.FixedSingle;
            box.BackColor = Color.FromArgb(18, 24, 24);
            box.ForeColor = Color.White;
            box.Font = new Font("Consolas", 13f, FontStyle.Bold, GraphicsUnit.Point);
            box.Text = value;
            box.TextAlign = HorizontalAlignment.Center;
            box.TextChanged += HexBoxChanged;
            box.Leave += HexBoxLeave;
            box.KeyDown += HexBoxKeyDown;
            box.Click += HexBoxClick;
            return box;
        }

        private void AddSliderLabels()
        {
            AddSliderLabel("A", 24, 2, 70);
            AddSliderLabel("B", 226, 2, 70);
            AddSliderLabel("R", 198, 70, 18);
            AddSliderLabel("G", 198, 116, 18);
            AddSliderLabel("B", 198, 162, 18);
            AddSliderLabel("R", 400, 70, 18);
            AddSliderLabel("G", 400, 116, 18);
            AddSliderLabel("B", 400, 162, 18);
            AddSliderLabel("HEX", 24, 202, 70);
            AddSliderLabel("HEX", 226, 202, 70);
            UpdateColorPreviews();
        }

        private void AddSliderLabel(string text, int x, int y, int width)
        {
            Label label = new Label();
            label.AutoSize = false;
            label.SetBounds(x, y, width, 18);
            label.BackColor = Color.Transparent;
            label.ForeColor = Color.White;
            label.Font = new Font("Arial Black", 8f, FontStyle.Bold, GraphicsUnit.Point);
            label.Text = text;
            settingsPanel.Controls.Add(label);
        }

        private void StartClicked(object sender, EventArgs e)
        {
            if (running)
            {
                return;
            }

            StopAllTimers();
            running = true;
            remaining = TotalSeconds;
            postPhaseRemaining = 0;
            heatSoundPlayed = false;
            postPhaseName = "";

            SetDefaultColors();
            phaseLabel.Text = "DAB TIME";
            timeLabel.Text = FormatTime(remaining);
            startButton.Enabled = false;
            stopButton.Enabled = true;
            startDelayTimer.Start();
        }

        private void BeginHeating(object sender, EventArgs e)
        {
            startDelayTimer.Stop();
            if (!running)
            {
                return;
            }

            SetPhase("HEAT", heatingForeground);
            CountdownTick(sender, e);
            countdownTimer.Start();
        }

        private void StopClicked(object sender, EventArgs e)
        {
            ResetTimer();
        }

        private void CountdownTick(object sender, EventArgs e)
        {
            if (!running)
            {
                return;
            }

            timeLabel.Text = FormatTime(remaining);

            if (remaining <= 0)
            {
                Finish();
                return;
            }

            if (remaining == HeatFinishedRemaining && !heatSoundPlayed)
            {
                heatSoundPlayed = true;
                SetPhase("COOLING", coolingForeground);
                PlaySound("DabTimer.Assets.heat_finished.wav");
            }

            if (remaining == GetReadyRemaining)
            {
                SetPhase("GET READY", dropForeground);
            }

            remaining -= 1;
        }

        private void Finish()
        {
            countdownTimer.Stop();
            PlaySound("DabTimer.Assets.timer_finished.wav");

            TopMost = true;
            Activate();
            TopMost = false;

            startButton.Enabled = false;
            stopButton.Enabled = false;
            StartPostPhase("DROP DAB");
        }

        private void StartPostPhase(string phaseName)
        {
            postPhaseTimer.Stop();
            postPhaseName = phaseName;
            postPhaseRemaining = phaseName == "DROP DAB" ? DropDabSeconds : DabTimePostSeconds;
            SetPhase(phaseName, phaseName == "DROP DAB" ? dropForeground : dabbingForeground);
            PostPhaseTick(this, EventArgs.Empty);
            postPhaseTimer.Start();
        }

        private void PostPhaseTick(object sender, EventArgs e)
        {
            timeLabel.Text = FormatTime(postPhaseRemaining);

            if (postPhaseRemaining <= 0)
            {
                if (postPhaseName == "DROP DAB")
                {
                    StartPostPhase("DAB TIME");
                    return;
                }

                Close();
                return;
            }

            postPhaseRemaining -= 1;
        }

        private void ResetTimer()
        {
            StopAllTimers();
            running = false;
            remaining = TotalSeconds;
            postPhaseRemaining = 0;
            heatSoundPlayed = false;
            postPhaseName = "";

            SetDefaultColors();
            phaseLabel.Text = "DAB TIME";
            timeLabel.Text = FormatTime(remaining);
            startButton.Enabled = true;
            stopButton.Enabled = false;
        }

        private void StopAllTimers()
        {
            startDelayTimer.Stop();
            countdownTimer.Stop();
            postPhaseTimer.Stop();
        }

        private void SetDefaultColors()
        {
            if (phaseAnimationTimer != null)
            {
                phaseAnimationTimer.Stop();
            }

            phaseFadingOut = false;
            phaseLabel.ForeColor = defaultForeground;
            phaseLabel.TextOpacity = 1f;
            timeLabel.ForeColor = defaultForeground;
            Invalidate();
        }

        private void SetPhase(string phase, Color color)
        {
            if (phaseLabel.Text == phase)
            {
                phaseLabel.ForeColor = color;
                phaseLabel.TextOpacity = 1f;
                phaseAnimationTimer.Stop();
                phaseFadingOut = false;
            }
            else
            {
                pendingPhaseText = phase;
                pendingPhaseColor = color;
                phaseFadingOut = true;
                if (!phaseAnimationTimer.Enabled)
                {
                    phaseAnimationTimer.Start();
                }
            }

            timeLabel.ForeColor = color;
        }

        private void PhaseAnimationTick(object sender, EventArgs e)
        {
            const float step = 0.12f;

            if (phaseFadingOut)
            {
                phaseLabel.TextOpacity = Math.Max(0f, phaseLabel.TextOpacity - step);
                if (phaseLabel.TextOpacity > 0f)
                {
                    return;
                }

                phaseLabel.Text = pendingPhaseText;
                phaseLabel.ForeColor = pendingPhaseColor;
                phaseFadingOut = false;
            }

            phaseLabel.TextOpacity = Math.Min(1f, phaseLabel.TextOpacity + step);
            if (phaseLabel.TextOpacity >= 1f)
            {
                phaseAnimationTimer.Stop();
            }
        }

        private void ToggleSettings(object sender, EventArgs e)
        {
            settingsPanel.Visible = !settingsPanel.Visible;
            settingsButton.Text = settingsPanel.Visible ? "x" : "+";
        }

        private void ToggleVolume(object sender, EventArgs e)
        {
            volumePanel.Visible = !volumePanel.Visible;
        }

        private void CloseClicked(object sender, EventArgs e)
        {
            Close();
        }

        private void ColorSliderChanged(object sender, EventArgs e)
        {
            if (updatingColorFields)
            {
                return;
            }

            gradientStart = Color.FromArgb(startRedSlider.Value, startGreenSlider.Value, startBlueSlider.Value);
            gradientEnd = Color.FromArgb(endRedSlider.Value, endGreenSlider.Value, endBlueSlider.Value);
            UpdateColorPreviews();
            StartGradientAnimation();
            SaveSettings();
        }

        private void VolumeSliderChanged(object sender, EventArgs e)
        {
            soundVolume = volumeSlider.Value;
            UpdateVolumeLabel();
            SaveSettings();
        }

        private void UpdateVolumeLabel()
        {
            if (volumeLabel != null)
            {
                volumeLabel.Text = soundVolume + "%";
            }
        }

        private void UpdateColorPreviews()
        {
            startPreview.BackColor = gradientStart;
            endPreview.BackColor = gradientEnd;
            UpdateHexBoxes();
        }

        private void UpdateHexBoxes()
        {
            if (startHexBox == null || endHexBox == null)
            {
                return;
            }

            updatingColorFields = true;
            startHexBox.Text = ColorToHex(gradientStart);
            endHexBox.Text = ColorToHex(gradientEnd);
            startHexBox.BackColor = Color.FromArgb(18, 24, 24);
            endHexBox.BackColor = Color.FromArgb(18, 24, 24);
            updatingColorFields = false;
        }

        private void HexBoxChanged(object sender, EventArgs e)
        {
            if (updatingColorFields)
            {
                return;
            }

            TextBox box = sender as TextBox;
            Color parsed;
            if (box == null || !TryParseColor(box.Text, out parsed))
            {
                if (box != null)
                {
                    box.BackColor = Color.FromArgb(62, 18, 24);
                }
                return;
            }

            box.BackColor = Color.FromArgb(18, 24, 24);
            if (box == startHexBox)
            {
                gradientStart = parsed;
            }
            else
            {
                gradientEnd = parsed;
            }

            SyncSlidersToColors();
            StartGradientAnimation();
            SaveSettings();
        }

        private void HexBoxLeave(object sender, EventArgs e)
        {
            TextBox box = sender as TextBox;
            Color parsed;
            if (box == null || !TryParseColor(box.Text, out parsed))
            {
                UpdateHexBoxes();
                return;
            }

            updatingColorFields = true;
            box.Text = ColorToHex(parsed);
            updatingColorFields = false;
        }

        private void HexBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                HexBoxLeave(sender, EventArgs.Empty);
                e.SuppressKeyPress = true;
            }
        }

        private void HexBoxClick(object sender, EventArgs e)
        {
            TextBox box = sender as TextBox;
            if (box != null)
            {
                box.SelectAll();
            }
        }

        private void SyncSlidersToColors()
        {
            updatingColorFields = true;
            startRedSlider.Value = gradientStart.R;
            startGreenSlider.Value = gradientStart.G;
            startBlueSlider.Value = gradientStart.B;
            endRedSlider.Value = gradientEnd.R;
            endGreenSlider.Value = gradientEnd.G;
            endBlueSlider.Value = gradientEnd.B;
            startPreview.BackColor = gradientStart;
            endPreview.BackColor = gradientEnd;
            updatingColorFields = false;
        }

        private void StartGradientAnimation()
        {
            if (!gradientAnimationTimer.Enabled)
            {
                gradientAnimationTimer.Start();
            }
        }

        private void GradientAnimationTick(object sender, EventArgs e)
        {
            renderedGradientStart = EaseColor(renderedGradientStart, gradientStart, 0.18);
            renderedGradientEnd = EaseColor(renderedGradientEnd, gradientEnd, 0.18);
            Invalidate();

            if (ColorsClose(renderedGradientStart, gradientStart) && ColorsClose(renderedGradientEnd, gradientEnd))
            {
                renderedGradientStart = gradientStart;
                renderedGradientEnd = gradientEnd;
                gradientAnimationTimer.Stop();
                Invalidate();
            }
        }

        private static Color EaseColor(Color current, Color target, double amount)
        {
            return Color.FromArgb(
                EaseChannel(current.R, target.R, amount),
                EaseChannel(current.G, target.G, amount),
                EaseChannel(current.B, target.B, amount));
        }

        private static int EaseChannel(int current, int target, double amount)
        {
            return current + (int)Math.Round((target - current) * amount);
        }

        private static bool ColorsClose(Color current, Color target)
        {
            return Math.Abs(current.R - target.R) <= 1 &&
                Math.Abs(current.G - target.G) <= 1 &&
                Math.Abs(current.B - target.B) <= 1;
        }

        private void LoadSettings()
        {
            if (!File.Exists(settingsPath))
            {
                return;
            }

            string[] lines = File.ReadAllLines(settingsPath);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split('=');
                if (parts.Length != 2)
                {
                    continue;
                }

                if (parts[0] == "gradientStart")
                {
                    Color parsed;
                    if (TryParseColor(parts[1], out parsed))
                    {
                        gradientStart = parsed;
                    }
                }
                else if (parts[0] == "gradientEnd")
                {
                    Color parsed;
                    if (TryParseColor(parts[1], out parsed))
                    {
                        gradientEnd = parsed;
                    }
                }
                else if (parts[0] == "soundVolume")
                {
                    int parsedVolume;
                    if (int.TryParse(parts[1], out parsedVolume))
                    {
                        soundVolume = Math.Max(0, Math.Min(100, parsedVolume));
                    }
                }
            }
        }

        private void SaveSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            File.WriteAllLines(settingsPath, new string[]
            {
                "gradientStart=" + ColorToHex(gradientStart),
                "gradientEnd=" + ColorToHex(gradientEnd),
                "soundVolume=" + soundVolume
            });
        }

        private static bool TryParseColor(string value, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            value = value.Trim();
            if (value.Length == 6)
            {
                value = "#" + value;
            }

            if (value.Length != 7 || value[0] != '#')
            {
                return false;
            }

            int red;
            int green;
            int blue;
            if (!int.TryParse(value.Substring(1, 2), System.Globalization.NumberStyles.HexNumber, null, out red) ||
                !int.TryParse(value.Substring(3, 2), System.Globalization.NumberStyles.HexNumber, null, out green) ||
                !int.TryParse(value.Substring(5, 2), System.Globalization.NumberStyles.HexNumber, null, out blue))
            {
                return false;
            }

            color = Color.FromArgb(red, green, blue);
            return true;
        }

        private static string ColorToHex(Color color)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        private static Font CreateDisplayFont(float size)
        {
            string[] names = new string[]
            {
                "Bahnschrift SemiCondensed",
                "Bahnschrift Condensed",
                "Arial Black",
                "Impact"
            };

            for (int i = 0; i < names.Length; i++)
            {
                try
                {
                    return new Font(names[i], size, FontStyle.Bold, GraphicsUnit.Point);
                }
                catch (ArgumentException)
                {
                }
            }

            return new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Point);
        }

        private void PlaySound(string resourceName)
        {
            string soundPath = ExtractSoundResource(resourceName);
            if (soundPath == null)
            {
                return;
            }

            string alias = "dabtimer_" + Guid.NewGuid().ToString("N");
            int mediaVolume = Math.Max(0, Math.Min(1000, soundVolume * 10));
            mciSendString("open \"" + soundPath + "\" type waveaudio alias " + alias, null, 0, IntPtr.Zero);
            mciSendString("setaudio " + alias + " volume to " + mediaVolume, null, 0, IntPtr.Zero);
            mciSendString("play " + alias + " from 0", null, 0, IntPtr.Zero);

            Timer cleanupTimer = new Timer();
            cleanupTimer.Interval = 12000;
            cleanupTimer.Tick += delegate
            {
                cleanupTimer.Stop();
                cleanupTimer.Dispose();
                mciSendString("close " + alias, null, 0, IntPtr.Zero);
            };
            cleanupTimer.Start();
        }

        private string ExtractSoundResource(string resourceName)
        {
            Directory.CreateDirectory(soundCacheDirectory);
            string fileName = resourceName.EndsWith("heat_finished.wav", StringComparison.OrdinalIgnoreCase)
                ? "heat_finished.wav"
                : "timer_finished.wav";
            string outputPath = Path.Combine(soundCacheDirectory, fileName);

            if (File.Exists(outputPath))
            {
                return outputPath;
            }

            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    return null;
                }

                using (FileStream output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(output);
                }
            }

            return outputPath;
        }

        private static Icon LoadIconResource()
        {
            Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DabTimer.Assets.app.ico");
            if (resourceStream == null)
            {
                return null;
            }

            return new Icon(resourceStream);
        }

        private static string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return string.Format("{0}:{1:00}", minutes, seconds);
        }
    }

    internal sealed class GlowLabel : Label
    {
        private float textOpacity = 1f;

        public GlowLabel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            UseCompatibleTextRendering = true;
        }

        public float TextOpacity
        {
            get
            {
                return textOpacity;
            }
            set
            {
                float clamped = Math.Max(0f, Math.Min(1f, value));
                if (Math.Abs(textOpacity - clamped) < 0.001f)
                {
                    return;
                }

                textOpacity = clamped;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.FormatFlags = StringFormatFlags.NoClip;

                RectangleF shadowRect = new RectangleF(ClientRectangle.X + 4, ClientRectangle.Y + 5, ClientRectangle.Width, ClientRectangle.Height);
                using (SolidBrush shadow = new SolidBrush(Color.FromArgb((int)(150 * textOpacity), 0, 0, 0)))
                {
                    e.Graphics.DrawString(Text, Font, shadow, shadowRect, format);
                }

                using (SolidBrush foreground = new SolidBrush(Color.FromArgb((int)(255 * textOpacity), ForeColor)))
                {
                    e.Graphics.DrawString(Text, Font, foreground, ClientRectangle, format);
                }
            }
        }
    }

    internal sealed class ColorSlider : Control
    {
        private int value;
        public event EventHandler ValueChanged;

        public ColorSlider()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        public int Value
        {
            get
            {
                return value;
            }
            set
            {
                int clamped = Math.Max(0, Math.Min(255, value));
                if (this.value == clamped)
                {
                    return;
                }

                this.value = clamped;
                Invalidate();
                if (ValueChanged != null)
                {
                    ValueChanged(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

            int trackY = Height / 2 - 4;
            Rectangle track = new Rectangle(8, trackY, Width - 16, 8);
            using (GraphicsPath path = RoundedRect(track, 4))
            using (SolidBrush background = new SolidBrush(Color.FromArgb(88, 255, 255, 255)))
            {
                e.Graphics.FillPath(background, path);
            }

            int fillWidth = Math.Max(8, (int)Math.Round(track.Width * (Value / 255.0)));
            Rectangle fill = new Rectangle(track.X, track.Y, fillWidth, track.Height);
            using (GraphicsPath path = RoundedRect(fill, 4))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
            {
                e.Graphics.FillPath(brush, path);
            }

            int knobX = track.X + (int)Math.Round(track.Width * (Value / 255.0));
            Rectangle knob = new Rectangle(knobX - 9, Height / 2 - 9, 18, 18);
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
            {
                e.Graphics.FillEllipse(shadow, knob.X + 2, knob.Y + 3, knob.Width, knob.Height);
            }
            using (SolidBrush knobBrush = new SolidBrush(Color.White))
            {
                e.Graphics.FillEllipse(knobBrush, knob);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            UpdateValueFromMouse(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.Left)
            {
                UpdateValueFromMouse(e.X);
            }
        }

        private void UpdateValueFromMouse(int x)
        {
            int left = 8;
            int width = Width - 16;
            double ratio = (x - left) / (double)width;
            Value = (int)Math.Round(Math.Max(0, Math.Min(1, ratio)) * 255);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class VolumeSlider : Control
    {
        private int value;
        public event EventHandler ValueChanged;

        public VolumeSlider()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        public int Value
        {
            get
            {
                return value;
            }
            set
            {
                int clamped = Math.Max(0, Math.Min(100, value));
                if (this.value == clamped)
                {
                    return;
                }

                this.value = clamped;
                Invalidate();
                if (ValueChanged != null)
                {
                    ValueChanged(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

            Rectangle track = new Rectangle(Width / 2 - 4, 8, 8, Height - 16);
            using (GraphicsPath path = RoundedRect(track, 4))
            using (SolidBrush background = new SolidBrush(Color.FromArgb(88, 255, 255, 255)))
            {
                e.Graphics.FillPath(background, path);
            }

            int fillHeight = Math.Max(8, (int)Math.Round(track.Height * (Value / 100.0)));
            Rectangle fill = new Rectangle(track.X, track.Bottom - fillHeight, track.Width, fillHeight);
            using (GraphicsPath path = RoundedRect(fill, 4))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(225, 255, 255, 255)))
            {
                e.Graphics.FillPath(brush, path);
            }

            int knobY = track.Bottom - (int)Math.Round(track.Height * (Value / 100.0));
            Rectangle knob = new Rectangle(Width / 2 - 10, knobY - 10, 20, 20);
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
            {
                e.Graphics.FillEllipse(shadow, knob.X + 2, knob.Y + 3, knob.Width, knob.Height);
            }
            using (SolidBrush knobBrush = new SolidBrush(Color.White))
            {
                e.Graphics.FillEllipse(knobBrush, knob);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            UpdateValueFromMouse(e.Y);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.Left)
            {
                UpdateValueFromMouse(e.Y);
            }
        }

        private void UpdateValueFromMouse(int y)
        {
            int top = 8;
            int height = Height - 16;
            double ratio = 1 - ((y - top) / (double)height);
            Value = (int)Math.Round(Math.Max(0, Math.Min(1, ratio)) * 100);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class SpeakerButton : Control
    {
        private bool hover;

        public SpeakerButton()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

            if (hover)
            {
                using (SolidBrush hoverBrush = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
                {
                    e.Graphics.FillEllipse(hoverBrush, 0, 0, Width - 1, Height - 1);
                }
            }

            using (Pen pen = new Pen(Color.White, 2f))
            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                Point[] body = new Point[]
                {
                    new Point(8, 14),
                    new Point(13, 14),
                    new Point(20, 8),
                    new Point(20, 26),
                    new Point(13, 20),
                    new Point(8, 20)
                };
                e.Graphics.FillPolygon(brush, body);
                e.Graphics.DrawArc(pen, 20, 11, 8, 12, -45, 90);
                e.Graphics.DrawArc(pen, 21, 7, 13, 20, -45, 90);
            }
        }
    }
}
