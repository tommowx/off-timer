using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OffTimer
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Contains("--verify")) return Checks.Run();
            try
            {
                var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
                app.Run(new MainWindow());
                return 0;
            }
            catch (Exception ex)
            {
                File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Off-Timer-startup-error.txt"), ex.ToString());
                MessageBox.Show("Off-Timer non è riuscita ad aprirsi.\n\n" + ex.Message, "Off-Timer", MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
        }
    }

    internal sealed class MainWindow : Window
    {
        private Brush Ink = Brush("#202723");
        private Brush Muted = Brush("#68716D");
        private Brush Accent = Brush("#28745E");
        private Brush Light = Brush("#F0F2EF");
        private Brush Surface = Brush("#FAFBF9");
        private readonly TextBox[] _time = new TextBox[4];
        private readonly TextBlock _error;
        private readonly TextBlock _countdown;
        private readonly TextBlock _due;
        private readonly TextBlock _notice;
        private readonly Button _start;
        private readonly Button _cancel;
        private readonly Border _runningCard;
        private readonly Button[] _actions = new Button[3];
        private readonly Border[] _actionCards = new Border[3];
        private readonly Border[] _actionRings = new Border[3];
        private readonly Ellipse[] _actionDots = new Ellipse[3];
        private int _selectedAction;
        private readonly DispatcherTimer _timer;
        private DateTime _deadline;
        private bool _running;
        private bool _fullScreen;
        private Button _fullScreenButton;
        private Button _themeButton;
        private readonly List<Button> _modeButtons = new List<Button>();
        private Border _timeCard;
        private Grid _layout;
        private ScrollViewer _scroll;
        private StackPanel _footer;
        private bool _darkTheme;
        private Window _blackScreen;

        internal MainWindow()
        {
            Title = "Off-Timer"; Width = 460; Height = 740; MinWidth = 420; MinHeight = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen; Background = Light; Foreground = Ink;
            FontFamily = new FontFamily("Segoe UI"); ResizeMode = ResizeMode.CanMinimize;
            UseLayoutRounding = true; SnapsToDevicePixels = true; Icon = LoadIcon();

            var layout = new Grid { Margin = new Thickness(30, 23, 30, 24), Background = Light };
            _layout = layout;
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Content = layout;

            var header = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            header.Children.Add(Text("OFF-TIMER  /  TIMER DI SISTEMA", 11, Muted, FontWeights.Bold));
            header.Children.Add(Text("Programma un’azione", 24, Ink, FontWeights.SemiBold, new Thickness(0, 8, 0, 0)));
            header.Children.Add(Text("Il computer si fermerà allo scadere del conto.", 13, Muted, FontWeights.Normal, new Thickness(0, 5, 0, 0)));
            Grid.SetRow(header, 0); layout.Children.Add(header);

            var content = new StackPanel();
            _timeCard = Card(new Thickness(0, 7, 0, 7));
            _timeCard.Child = new StackPanel();
            var timePanel = (StackPanel)_timeCard.Child;
            timePanel.Children.Add(Text("DURATA", 11, Muted, FontWeights.Bold, new Thickness(1, 0, 0, 12)));
            var timeGrid = new Grid();
            for (int i = 0; i < 7; i++) timeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = i % 2 == 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(10) });
            string[] units = { "giorni", "ore", "minuti", "secondi" };
            int[] max = { 3650, 23, 59, 59 };
            for (int i = 0; i < 4; i++)
            {
                var part = new StackPanel();
                var input = new TextBox { Text = "0", FontSize = 25, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center,
                    BorderThickness = new Thickness(0), Background = Brushes.Transparent, MaxLength = 4, Padding = new Thickness(0, 4, 0, 2),
                    VerticalContentAlignment = VerticalAlignment.Center, CaretBrush = Accent };
                AutomationProperties.SetName(input, units[i]);
                input.PreviewTextInput += (s, e) => e.Handled = !Regex.IsMatch(e.Text, "^\\d+$");
                DataObject.AddPastingHandler(input, (s, e) => { string v = e.DataObject.GetData(DataFormats.Text) as string; if (v == null || !Regex.IsMatch(v, "^\\d+$")) e.CancelCommand(); });
                input.TextChanged += (s, e) => ValidateDuration();
                _time[i] = input; part.Children.Add(input);
                part.Children.Add(Text(units[i], 11, Muted, FontWeights.Normal, new Thickness(0, 4, 0, 0), HorizontalAlignment.Center));
                Grid.SetColumn(part, i * 2); timeGrid.Children.Add(part);
                if (i < 3) { var sep = Text(":", 20, Brush("#A0A3A9"), FontWeights.Normal, new Thickness(0, 7, 0, 0), HorizontalAlignment.Center); Grid.SetColumn(sep, i * 2 + 1); timeGrid.Children.Add(sep); }
            }
            timePanel.Children.Add(timeGrid);
            _error = Text("", 12, Brush("#C93434"), FontWeights.Normal, new Thickness(1, 9, 0, 0));
            _error.TextWrapping = TextWrapping.Wrap; _error.Visibility = Visibility.Collapsed; timePanel.Children.Add(_error);
            content.Children.Add(_timeCard);
            content.Children.Add(Text("ALLO SCADERE", 11, Muted, FontWeights.Bold, new Thickness(2, 13, 0, 5)));

            string[] labels = { "Spegni il computer", "Riavvia", "Sospendi" };
            string[] details = { "Chiude Windows e arresta il PC", "Chiude Windows e lo avvia di nuovo", "Riduce al minimo il consumo di energia" };
            for (int i = 0; i < 3; i++) content.Children.Add(ActionCard(i, labels[i], details[i]));

            _countdown = Text("00:00:00", 38, Ink, FontWeights.SemiBold, new Thickness(0, 4, 0, 5), HorizontalAlignment.Center);
            _due = Text("", 12, Muted, FontWeights.Normal, new Thickness(0), HorizontalAlignment.Center);
            _runningCard = Card(new Thickness(0, 13, 0, 0)); _runningCard.Visibility = Visibility.Collapsed;
            var running = new StackPanel(); running.Children.Add(Text("Timer attivo", 13, Muted, FontWeights.Normal, new Thickness(0), HorizontalAlignment.Center));
            running.Children.Add(_countdown); running.Children.Add(_due); _runningCard.Child = running; content.Children.Add(_runningCard);

            var scroll = new ScrollViewer { Content = content, Background = Light, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _scroll = scroll; Grid.SetRow(scroll, 1); layout.Children.Add(scroll);

            var footer = new StackPanel { Margin = new Thickness(0, 13, 0, 0), Background = Light };
            _footer = footer;
            _notice = Text("Ti chiederemo conferma prima di programmare.", 11, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 10), HorizontalAlignment.Center);
            _notice.TextAlignment = TextAlignment.Center; _notice.TextWrapping = TextWrapping.Wrap; footer.Children.Add(_notice);
            _start = new Button { Content = "Avvia timer", Background = Accent, Foreground = Brushes.White, Padding = new Thickness(16, 13, 16, 13),
                FontSize = 14, FontWeight = FontWeights.SemiBold, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
            _start.Click += StartTimer; footer.Children.Add(_start);
            _cancel = new Button { Content = "Annulla timer", Background = Brush("#E9E9EB"), Foreground = Brush("#24262A"), Padding = new Thickness(16, 12, 16, 12),
                FontSize = 14, FontWeight = FontWeights.SemiBold, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 8, 0, 0) };
            _cancel.Click += CancelTimer; footer.Children.Add(_cancel);
            var displayModes = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            for (int i = 0; i < 5; i++) displayModes.ColumnDefinitions.Add(new ColumnDefinition { Width = i % 2 == 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(8) });
            _fullScreenButton = ModeButton("Schermo intero");
            _fullScreenButton.Click += (s, e) => ToggleFullScreen();
            Grid.SetColumn(_fullScreenButton, 0); displayModes.Children.Add(_fullScreenButton);
            var blackButton = ModeButton("Schermo nero");
            blackButton.Click += (s, e) => ShowBlackScreen();
            Grid.SetColumn(blackButton, 2); displayModes.Children.Add(blackButton);
            _themeButton = ModeButton("Tema nero");
            _themeButton.Click += (s, e) => ToggleTheme();
            Grid.SetColumn(_themeButton, 4); displayModes.Children.Add(_themeButton);
            footer.Children.Add(displayModes);
            Grid.SetRow(footer, 2); layout.Children.Add(footer);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _timer.Tick += (s, e) => UpdateCountdown();
            UpdateActionCards();
            ValidateDuration();
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.F11) { ToggleFullScreen(); e.Handled = true; }
                else if (e.Key == Key.Escape && _fullScreen) { ToggleFullScreen(); e.Handled = true; }
            };
        }

        private static Brush Brush(string hex) { return (Brush)new BrushConverter().ConvertFromString(hex); }
        private static ImageSource LoadIcon()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("off-timer-icon.png");
            if (stream == null) return null;
            using (stream) return BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        }
        private TextBlock Text(string value, double size, Brush color, FontWeight weight, Thickness? margin = null, HorizontalAlignment align = HorizontalAlignment.Left)
        { return new TextBlock { Text = value, FontSize = size, Foreground = color, Tag = Object.ReferenceEquals(color, Ink) ? "ink" : Object.ReferenceEquals(color, Muted) ? "muted" : null, FontWeight = weight, Margin = margin ?? new Thickness(0), HorizontalAlignment = align }; }
        private Border Card(Thickness margin)
        { return new Border { Background = Surface, CornerRadius = new CornerRadius(9), Padding = new Thickness(18), Margin = margin }; }

        private Button ModeButton(string label)
        {
            var button = new Button { Content = label, Background = Brush("#E4E8E4"), Foreground = Brush("#34413A"), Padding = new Thickness(8, 9, 8, 9),
                FontSize = 11, FontWeight = FontWeights.SemiBold, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
            _modeButtons.Add(button); return button;
        }

        internal bool IsDarkTheme { get { return _darkTheme; } }
        internal string SurfaceColor { get { return _timeCard.Background.ToString(); } }

        internal void ToggleTheme()
        {
            _darkTheme = !_darkTheme;
            Ink = Brush(_darkTheme ? "#F0F0F0" : "#202723");
            Muted = Brush(_darkTheme ? "#A0A0A0" : "#68716D");
            Accent = Brush(_darkTheme ? "#D6D6D6" : "#28745E");
            Light = Brush(_darkTheme ? "#000000" : "#F0F2EF");
            Surface = Brush(_darkTheme ? "#000000" : "#FAFBF9");
            Background = Light; Foreground = Ink;
            _layout.Background = Light; _scroll.Background = Light; _footer.Background = Light;
            _timeCard.Background = Surface; _runningCard.Background = Surface;
            _start.Background = Accent;
            _start.Foreground = Brush(_darkTheme ? "#050505" : "#FFFFFF");
            _cancel.Background = Brush(_darkTheme ? "#171717" : "#E9E9EB");
            _cancel.Foreground = Ink;
            foreach (var input in _time) { input.Foreground = Ink; input.CaretBrush = Accent; }
            foreach (var button in _modeButtons)
            {
                button.Background = Brush(_darkTheme ? "#171717" : "#E4E8E4");
                button.Foreground = Brush(_darkTheme ? "#E8E8E8" : "#34413A");
            }
            _themeButton.Content = _darkTheme ? "Tema chiaro" : "Tema nero";
            for (int i = 0; i < _actionCards.Length; i++)
            {
                _actionCards[i].Background = Brush(_darkTheme ? (i == _selectedAction ? "#111111" : "#000000") : (i == _selectedAction ? "#EEF4F0" : "#FAFBF9"));
                _actionCards[i].BorderBrush = Brush(_darkTheme ? (i == _selectedAction ? "#777777" : "#292929") : (i == _selectedAction ? "#78A18F" : "#DDE2DD"));
                _actionRings[i].Background = Brush(_darkTheme ? "#000000" : "#FAFBF9");
                _actionRings[i].BorderBrush = i == _selectedAction ? Accent : Brush(_darkTheme ? "#666666" : "#BBC3BD");
                _actionDots[i].Fill = Accent;
            }
            UpdateTextColors(this);
        }

        private void UpdateTextColors(DependencyObject parent)
        {
            var text = parent as TextBlock;
            if (text != null)
            {
                if ((string)text.Tag == "ink") text.Foreground = Ink;
                else if ((string)text.Tag == "muted") text.Foreground = Muted;
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) UpdateTextColors(VisualTreeHelper.GetChild(parent, i));
        }

        internal bool IsFullScreen { get { return _fullScreen; } }

        internal void ToggleFullScreen()
        {
            if (_blackScreen != null) return;
            if (!_fullScreen)
            {
                _fullScreen = true; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; WindowState = WindowState.Maximized;
                _fullScreenButton.Content = "Esci da schermo intero · Esc";
            }
            else
            {
                _fullScreen = false; WindowState = WindowState.Normal; WindowStyle = WindowStyle.SingleBorderWindow; ResizeMode = ResizeMode.CanMinimize;
                _fullScreenButton.Content = "Schermo intero";
            }
        }

        private void ShowBlackScreen()
        {
            var black = new Window { Title = "Off-Timer · Schermo nero", Background = Brushes.Black, Foreground = Brushes.White,
                WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, WindowState = WindowState.Maximized,
                Topmost = true, ShowInTaskbar = false, Cursor = Cursors.None };
            black.PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) { black.Close(); e.Handled = true; } };
            black.MouseDown += (s, e) => black.Close();
            black.Closed += (s, e) => { _blackScreen = null; Activate(); };
            _blackScreen = black; black.Show(); black.Activate();
        }

        private UIElement ActionCard(int index, string title, string description)
        {
            var card = new Border { Background = Surface, BorderBrush = Brush("#DDE2DD"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 13, 14, 13), Margin = new Thickness(0, 0, 0, 8) };
            var button = new Button { Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Center, FocusVisualStyle = null, Cursor = Cursors.Hand };
            AutomationProperties.SetName(button, title);
            AutomationProperties.SetHelpText(button, description);
            _actions[index] = button; _actionCards[index] = card;
            var row = new Grid(); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var ring = new Border { Width = 18, Height = 18, CornerRadius = new CornerRadius(9), BorderThickness = new Thickness(1.5), BorderBrush = Brush("#BBC3BD"), Background = Surface, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
            var dot = new Ellipse { Width = 8, Height = 8, Fill = Accent, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed };
            ring.Child = dot; Grid.SetColumn(ring, 0); row.Children.Add(ring);
            var panel = new StackPanel(); panel.Children.Add(Text(title, 14, Ink, FontWeights.SemiBold)); panel.Children.Add(Text(description, 12, Muted, FontWeights.Normal, new Thickness(0, 3, 0, 0)));
            Grid.SetColumn(panel, 2); row.Children.Add(panel); button.Content = row; card.Child = button;
            _actionRings[index] = ring; _actionDots[index] = dot;
            button.Click += (s, e) => { _selectedAction = index; UpdateActionCards(); };
            return card;
        }

        private void UpdateActionCards()
        {
            for (int i = 0; i < _actions.Length; i++)
            {
                bool selected = i == _selectedAction;
                _actionCards[i].BorderBrush = Brush(_darkTheme ? (selected ? "#777777" : "#292929") : (selected ? "#78A18F" : "#DDE2DD"));
                _actionCards[i].Background = Brush(_darkTheme ? (selected ? "#111111" : "#000000") : (selected ? "#EEF4F0" : "#FAFBF9"));
                _actionRings[i].BorderBrush = selected ? Accent : Brush(_darkTheme ? "#66716A" : "#BBC3BD");
                _actionRings[i].Background = Brush(_darkTheme ? "#000000" : "#FAFBF9");
                _actionDots[i].Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private bool TryDuration(out TimeSpan duration, out string error)
        {
            return TryCalculateDuration(_time.Select(input => input.Text).ToArray(), out duration, out error);
        }

        internal static bool TryCalculateDuration(string[] values, out TimeSpan duration, out string error)
        {
            duration = TimeSpan.Zero; error = null;
            int[] limit = { 3650, 23, 59, 59 }; long[] v = new long[4];
            if (values == null || values.Length != 4) { error = "Inserisci giorni, ore, minuti e secondi."; return false; }
            for (int i = 0; i < 4; i++)
            {
                string value = (values[i] ?? "0").Trim(); if (value.Length == 0) value = "0";
                if (!Regex.IsMatch(value, "^\\d{1,4}$") || !long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out v[i]) || v[i] > limit[i])
                { error = "Usa solo cifre: giorni (max 3650), ore (0–23), minuti e secondi (0–59)."; return false; }
            }
            duration = TimeSpan.FromDays(v[0]) + TimeSpan.FromHours(v[1]) + TimeSpan.FromMinutes(v[2]) + TimeSpan.FromSeconds(v[3]);
            if (duration <= TimeSpan.Zero) { error = "Scegli un tempo maggiore di zero."; return false; }
            if (duration.TotalSeconds > 315360000) { error = "La durata massima è di 10 anni."; return false; }
            return true;
        }

        private void ValidateDuration()
        {
            TimeSpan duration; string error;
            bool valid = TryDuration(out duration, out error);
            _error.Text = error ?? ""; _error.Visibility = valid || _running ? Visibility.Collapsed : Visibility.Visible;
            _start.IsEnabled = valid || _running;
        }

        private string SelectedAction()
        { return _selectedAction == 2 ? "sleep" : _selectedAction == 1 ? "restart" : "shutdown"; }

        internal static ActionInfo GetAction(string key)
        {
            switch (key)
            {
                case "shutdown": return new ActionInfo("spento", "shutdown.exe", "/s /t 0 /f");
                case "restart": return new ActionInfo("riavviato", "shutdown.exe", "/r /t 0 /f");
                case "sleep": return new ActionInfo("sospeso", null, null);
                default: throw new ArgumentOutOfRangeException("key");
            }
        }

        private void StartTimer(object sender, RoutedEventArgs e)
        {
            TimeSpan duration; string error;
            if (!TryDuration(out duration, out error)) { _error.Text = error; _error.Visibility = Visibility.Visible; return; }
            var action = GetAction(SelectedAction());
            string pretty = Format(duration);
            var answer = MessageBox.Show(this,
                "Tra " + pretty + " il PC verrà " + action.Label + ".\n\nSalva prima il lavoro aperto: Windows chiuderà le app alla scadenza.\n\nVuoi avviare il timer?",
                "Conferma timer", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (answer != MessageBoxResult.Yes) return;
            BeginCountdown(duration);
        }

        internal string CountdownText { get { return _countdown.Text; } }

        internal void BeginCountdown(TimeSpan duration)
        {
            _deadline = DateTime.UtcNow.Add(duration); _running = true; SetRunningUi(); UpdateCountdown(); _timer.Start();
        }

        internal void CancelTimer(object sender, RoutedEventArgs e)
        { _timer.Stop(); _running = false; SetRunningUi(); }

        private void SetRunningUi()
        {
            _runningCard.Visibility = _running ? Visibility.Visible : Visibility.Collapsed;
            _start.Visibility = _running ? Visibility.Collapsed : Visibility.Visible;
            _cancel.Visibility = _running ? Visibility.Visible : Visibility.Collapsed;
            foreach (var input in _time) input.IsEnabled = !_running;
            foreach (var action in _actions) action.IsEnabled = !_running;
            _notice.Text = _running ? "Off-Timer deve restare aperto. Puoi annullare il conto qui sotto." : "Ti chiederemo conferma prima di programmare.";
            ValidateDuration();
        }

        private void UpdateCountdown()
        {
            if (!_running) return;
            TimeSpan remaining = _deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                _timer.Stop(); _countdown.Text = "00:00:00";
                try { PerformAction(SelectedAction()); }
                catch (Exception ex) { _running = false; SetRunningUi(); MessageBox.Show(this, "Non è stato possibile completare l'operazione.\n\n" + ex.Message, "Azione non completata", MessageBoxButton.OK, MessageBoxImage.Error); }
                return;
            }
            _countdown.Text = Format(remaining);
            _due.Text = GetAction(SelectedAction()).Label + " · " + DateTime.Now.Add(remaining).ToString("dddd d MMMM, HH:mm", CultureInfo.GetCultureInfo("it-IT"));
        }

        internal static string Format(TimeSpan duration)
        { return duration.TotalDays >= 1 ? ((int)duration.TotalDays).ToString(CultureInfo.InvariantCulture) + " g  " + duration.ToString(@"hh\:mm\:ss") : ((int)duration.TotalHours).ToString("00", CultureInfo.InvariantCulture) + duration.ToString(@"\:mm\:ss"); }

        private static void PerformAction(string action)
        {
            if (action == "sleep")
            {
                if (!OffTimerPower.SetSuspendState(false, false, false)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                return;
            }
            ActionInfo info = GetAction(action);
            using (var process = Process.Start(new ProcessStartInfo(info.Executable, info.Arguments) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden }))
            {
                if (process == null || !process.WaitForExit(3000)) throw new InvalidOperationException("Windows non ha confermato l'avvio.");
                if (process.ExitCode != 0) throw new InvalidOperationException("Windows ha restituito il codice " + process.ExitCode + ".");
            }
        }

    }

    internal sealed class ActionInfo
    {
        internal readonly string Label, Executable, Arguments;
        internal ActionInfo(string label, string executable, string arguments) { Label = label; Executable = executable; Arguments = arguments; }
    }

    internal static class OffTimerPower
    {
        [DllImport("powrprof.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
    }

    internal static class Checks
    {
        internal static int Run()
        {
            int count = 0;
            if (MainWindow.Format(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(2)).Add(TimeSpan.FromSeconds(3))) != "01:02:03") throw new Exception("Formato durata errato."); count++;
            if (MainWindow.Format(TimeSpan.FromDays(3).Add(TimeSpan.FromHours(1))) != "3 g  01:00:00") throw new Exception("Formato giorni errato."); count++;
            if (MainWindow.GetAction("shutdown").Arguments != "/s /t 0 /f" || MainWindow.GetAction("restart").Arguments != "/r /t 0 /f" || MainWindow.GetAction("sleep").Executable != null) throw new Exception("Azione non associata correttamente."); count++;
            TimeSpan parsed; string error;
            if (!MainWindow.TryCalculateDuration(new[] { "2", "3", "4", "5" }, out parsed, out error) || parsed != TimeSpan.FromDays(2).Add(TimeSpan.FromHours(3)).Add(TimeSpan.FromMinutes(4)).Add(TimeSpan.FromSeconds(5))) throw new Exception("Calcolo durata errato."); count++;
            foreach (string[] invalid in new[] { new[] { "0", "24", "0", "0" }, new[] { "0", "0", "60", "0" }, new[] { "0", "0", "0", "60" }, new[] { "0", "0", "0", "0" }, new[] { "3651", "0", "0", "0" }, new[] { "x", "0", "0", "0" } })
                if (MainWindow.TryCalculateDuration(invalid, out parsed, out error)) throw new Exception("Un input non valido è stato accettato.");
            count++;
            var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
            var window = new MainWindow();
            if (window.Icon == null || window.Content == null || window.Title != "Off-Timer") throw new Exception("Finestra o icona non caricate.");
            window.Measure(new Size(460, 740)); window.Arrange(new Rect(0, 0, 460, 740)); window.UpdateLayout();
            window.ToggleFullScreen(); if (!window.IsFullScreen || window.WindowStyle != WindowStyle.None) throw new Exception("Schermo intero non attivato.");
            window.ToggleFullScreen(); if (window.IsFullScreen || window.WindowStyle == WindowStyle.None) throw new Exception("Uscita da schermo intero non riuscita.");
            window.ToggleTheme(); if (!window.IsDarkTheme || window.Background.ToString() != "#FF000000" || window.SurfaceColor != "#FF000000") throw new Exception("Tema nero non applicato a tutta la finestra.");
            window.ToggleTheme(); if (window.IsDarkTheme || window.Background.ToString() != "#FFF0F2EF") throw new Exception("Ripristino tema chiaro non riuscito.");
            window.BeginCountdown(TimeSpan.FromSeconds(4));
            TimeSpan before = TimeSpan.ParseExact(window.CountdownText, @"hh\:mm\:ss", CultureInfo.InvariantCulture);
            var frame = new DispatcherFrame();
            var wait = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1300) };
            wait.Tick += (s, e) => { wait.Stop(); frame.Continue = false; };
            wait.Start(); Dispatcher.PushFrame(frame);
            TimeSpan after = TimeSpan.ParseExact(window.CountdownText, @"hh\:mm\:ss", CultureInfo.InvariantCulture);
            window.CancelTimer(null, null);
            if (after >= before) throw new Exception("Il conto alla rovescia non diminuisce mentre il timer è attivo.");
            window.Close(); app.Shutdown(); count++;
            Console.WriteLine("Verifiche superate: " + count + ". Spegnimento, riavvio e sospensione non sono stati eseguiti.");
            return 0;
        }
    }
}
