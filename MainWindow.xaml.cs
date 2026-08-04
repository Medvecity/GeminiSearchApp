using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Data.OleDb;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices; 
using System.Data;
using System.IO; 
using Microsoft.Win32; // ÚJ: Registry olvasáshoz

namespace SearchApp;

public class SearchResult
{
    public string Title { get; set; }
    public string Path { get; set; }
    public ImageSource Icon { get; set; } 
}

public partial class MainWindow : Window
{
    private string geminiApiKey; 
    private readonly HttpClient httpClient = new HttpClient();
    
    // Többnyelvűséghez szükséges változók
    private string AppLang = "hu"; // Alapértelmezett
    private string PromptLangName = "magyarul"; 

    private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GeminiSearchApp");
    private static readonly string ApiKeyFilePath = Path.Combine(AppDataFolder, "apikey.txt");

    // --- WINDOWS API HÍVÁSOK ---
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] private static extern IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetSystemPowerStatus(SystemPowerStatus sps);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    public class SystemPowerStatus { public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag; public int BatteryLifeTime, BatteryFullLifeTime; }
    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORYSTATUSEX { public uint dwLength, dwMemoryLoad; public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual; }

    private const int HOTKEY_ID = 9000;
    private const uint MOD_ALT = 0x0001; 
    private const uint VK_SPACE = 0x20; 
    private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
    private const int WM_APPCOMMAND = 0x319;

    public MainWindow()
    {
        InitializeComponent();
        this.Top = 20;
        this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
        httpClient.Timeout = TimeSpan.FromMinutes(2);
        
        LoadLanguage(); // ATOMBOMBA START
        geminiApiKey = LoadApiKey();

        UpdateUILanguage();
    }

    private void UpdateUILanguage()
    {
        switch (AppLang)
        {
            case "en":
                FilterAll.Content = "All";
                FilterFiles.Content = "Files";
                FilterWeb.Content = "Web";
                // FilterAI.Content = "Gemini AI"; // Ha ezt is fixálni akarod
                if (SearchPlaceholder != null) SearchPlaceholder.Text = "Search for apps, files, or ask me (Gemini)...";
                break;
            case "de":
                FilterAll.Content = "Alles";
                FilterFiles.Content = "Dateien";
                FilterWeb.Content = "Web";
                if (SearchPlaceholder != null) SearchPlaceholder.Text = "Suche nach Apps, Dateien oder frage mich (Gemini)...";
                break;
            case "fr":
                FilterAll.Content = "Tout";
                FilterFiles.Content = "Fichiers";
                FilterWeb.Content = "Web"; // Vagy ha akarod, átírhatod "Internet"-re
                if (SearchPlaceholder != null) SearchPlaceholder.Text = "Recherchez des applis, fichiers ou demandez-moi (Gemini)...";
                break;
            default:
                FilterAll.Content = "Minden";
                FilterFiles.Content = "Fájlok";
                FilterWeb.Content = "Web";
                if (SearchPlaceholder != null) SearchPlaceholder.Text = "Keresés, programok, vagy kérdezz tőlem (Gemini)...";
                break;
        }
    }

    // --- TÖBBNYELVŰSÉG LOGIKÁJA ---
    // --- TÖBBNYELVŰSÉG LOGIKÁJA (ATOMBOMBA BIZTOS) ---
    private void LoadLanguage()
    {
        AppLang = "hu"; // Alapértelmezett, ha bármi baj lenne
        try
        {
            // Pont ugyanott keressük, ahol az apikey.txt is van!
            string configFile = Path.Combine(AppDataFolder, "config.ini");
            if (File.Exists(configFile))
            {
                string[] lines = File.ReadAllLines(configFile);
                foreach (string line in lines)
                {
                    if (line.Trim().StartsWith("Language="))
                    {
                        string lang = line.Split('=')[1].Trim().ToLower();
                        
                        // Ez megfogja azt is, ha véletlenül "english", vagy "en" lenne beírva!
                        if (lang.StartsWith("en")) AppLang = "en";
                        else if (lang.StartsWith("de")) AppLang = "de";
                        else if (lang.StartsWith("fr")) AppLang = "fr";
                        else AppLang = "hu";
                    }
                }
            }
        }
        catch { }
    }
    
    private string GetText(string key)
    {
        var dict = new Dictionary<string, Dictionary<string, string>>
        {
            ["Thinking"] = new() { ["hu"] = "Gemini gondolkodik... ✨", ["en"] = "Gemini is thinking... ✨", ["de"] = "Gemini denkt nach... ✨", ["fr"] = "Gemini réfléchit... ✨" },
            ["KeySaved"] = new() { ["hu"] = "Kulcs elmentve! ✨", ["en"] = "Key saved! ✨", ["de"] = "Schlüssel gespeichert! ✨", ["fr"] = "Clé enregistrée! ✨" },
            ["Opening"] = new() { ["hu"] = "megnyitása folyamatban... 🚀", ["en"] = "opening... 🚀", ["de"] = "wird geöffnet... 🚀", ["fr"] = "ouverture en cours... 🚀" },
            ["Closing"] = new() { ["hu"] = "bezárása folyamatban... 🛑", ["en"] = "closing... 🛑", ["de"] = "wird geschlossen... 🛑", ["fr"] = "fermeture en cours... 🛑" },
            ["WebOpened"] = new() { ["hu"] = "Weboldal megnyitva! 🌐", ["en"] = "Website opened! 🌐", ["de"] = "Webseite geöffnet! 🌐", ["fr"] = "Site web ouvert! 🌐" },
            ["Searching"] = new() { ["hu"] = "Keresés indítva! 🔍", ["en"] = "Search started! 🔍", ["de"] = "Suche gestartet! 🔍", ["fr"] = "Recherche lancée! 🔍" },
            ["NotFound"] = new() { ["hu"] = "Nem találtam meg a gépeden. 😔", ["en"] = "Could not find it on your PC. 😔", ["de"] = "Nicht auf deinem PC gefunden. 😔", ["fr"] = "Introuvable sur votre PC. 😔" },
            ["Welcome"] = new() { 
                ["hu"] = "A mesterséges intelligencia használatához add meg az ingyenes API kulcsodat.", 
                ["en"] = "Please enter your free API key to use the Artificial Intelligence.",
                ["de"] = "Bitte geben Sie Ihren kostenlosen API-Schlüssel ein, um die KI zu nutzen.",
                ["fr"] = "Veuillez entrer votre clé API gratuite pour utiliser l'IA."
            },
            ["GetLink"] = new() { ["hu"] = "Kattints ide a kulcs beszerzéséhez", ["en"] = "Click here to get a key", ["de"] = "Klicken Sie hier für den Schlüssel", ["fr"] = "Cliquez ici pour obtenir une clé" },
            ["SaveBtn"] = new() { ["hu"] = "Mentés", ["en"] = "Save", ["de"] = "Speichern", ["fr"] = "Sauvegarder" }
        };

        if (dict.ContainsKey(key) && dict[key].ContainsKey(AppLang)) return dict[key][AppLang];
        if (dict.ContainsKey(key) && dict[key].ContainsKey("hu")) return dict[key]["hu"];
        return key;
    }

    private static string LoadApiKey()
    {
        if (File.Exists(ApiKeyFilePath)) return File.ReadAllText(ApiKeyFilePath).Trim();
        return ""; 
    }

    public static void SaveApiKey(string key)
    {
        if (!Directory.Exists(AppDataFolder)) Directory.CreateDirectory(AppDataFolder);
        File.WriteAllText(ApiKeyFilePath, key.Trim());
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        IntPtr handle = new WindowInteropHelper(this).Handle;
        HwndSource source = HwndSource.FromHwnd(handle);
        source.AddHook(HwndHook);
        RegisterHotKey(handle, HOTKEY_ID, MOD_ALT, VK_SPACE);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            if (this.Visibility == Visibility.Visible) this.Visibility = Visibility.Hidden;
            else { this.Visibility = Visibility.Visible; this.Activate(); SearchInput.Focus(); }
            handled = true;
        }
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        UnregisterHotKey(new WindowInteropHelper(this).Handle, HOTKEY_ID);
        base.OnClosed(e);
    }

    private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = SearchInput.Text.Trim();
        if (string.IsNullOrEmpty(query) || FilterWeb.IsChecked == true || FilterAI.IsChecked == true)
        {
            SearchResultsList.ItemsSource = null; SearchResultsList.Visibility = Visibility.Collapsed; AiResponseScroll.Visibility = Visibility.Collapsed; return;
        }

        var results = new List<SearchResult>();
        try
        {
            using (var connection = new OleDbConnection(@"Provider=Search.CollatorDSO;Extended Properties=""Application=Windows"""))
            {
                connection.Open();
                using (var command = new OleDbCommand($"SELECT TOP 5 System.ItemNameDisplay, System.ItemPathDisplay FROM SystemIndex WHERE System.FileName LIKE '%{query}%'", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string title = reader[0].ToString(), path = reader[1].ToString();
                        if (!string.IsNullOrEmpty(path))
                        {
                            var result = new SearchResult { Title = title, Path = path };
                            try { using (var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(path)) if (sysicon != null) result.Icon = Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); } catch { }
                            results.Add(result);
                        }
                    }
                }
            }
        } catch { }

        if (results.Count > 0) { SearchResultsList.ItemsSource = results; SearchResultsList.Visibility = Visibility.Visible; ExpandWindow(); }
        else { SearchResultsList.ItemsSource = null; SearchResultsList.Visibility = Visibility.Collapsed; }
    }

    private void SearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SearchResultsList.SelectedItem is SearchResult selected)
        {
            try { Process.Start(new ProcessStartInfo(selected.Path) { UseShellExecute = true }); ResetSearch(); this.Visibility = Visibility.Hidden; } catch { }
        }
    }

    private async void SearchInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            string query = SearchInput.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            bool isGeminiKeyword = false;
            string aiQuery = query;

            if (query.StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
            {
                if (query.Length == 6 || !char.IsLetterOrDigit(query[6]))
                {
                    isGeminiKeyword = true;
                    aiQuery = query.Substring(6).TrimStart(' ', ',', ':', '-', '.').Trim();
                }
            }

            if (FilterAI.IsChecked == true || (FilterAll.IsChecked == true && isGeminiKeyword))
            {
                if (string.IsNullOrEmpty(aiQuery) && FilterAI.IsChecked == false) return; 
                if (string.IsNullOrEmpty(geminiApiKey)) { PromptForApiKey(); return; }

                SearchInput.IsEnabled = false; SearchResultsList.Visibility = Visibility.Collapsed;
                AiResponseText.Text = GetText("Thinking");
                AiResponseScroll.Visibility = Visibility.Visible;
                ExpandWindow();
                await AskGemini(aiQuery);
                SearchInput.IsEnabled = true; SearchInput.Focus(); return; 
            }

            if (FilterWeb.IsChecked == true) { Process.Start(new ProcessStartInfo("https://www.google.com/search?q=" + Uri.EscapeDataString(query)) { UseShellExecute = true }); ResetSearch(); this.Visibility = Visibility.Hidden; return; }
            if (FilterFiles.IsChecked == true || FilterAll.IsChecked == true)
            {
                if (SearchResultsList.Items.Count > 0)
                {
                    try { Process.Start(new ProcessStartInfo((SearchResultsList.Items[0] as SearchResult).Path) { UseShellExecute = true }); ResetSearch(); this.Visibility = Visibility.Hidden; } catch { }
                }
                else if (FilterAll.IsChecked == true) { Process.Start(new ProcessStartInfo("https://www.google.com/search?q=" + Uri.EscapeDataString(query)) { UseShellExecute = true }); ResetSearch(); this.Visibility = Visibility.Hidden; }
            }
        }
    }

    private void PromptForApiKey()
    {
        Window prompt = new Window() { Title = "Gemini Search App", Width = 450, Height = 250, WindowStartupLocation = WindowStartupLocation.CenterScreen, ResizeMode = ResizeMode.NoResize, Topmost = true };
        StackPanel stack = new StackPanel() { Margin = new Thickness(20) };
        stack.Children.Add(new TextBlock() { Text = GetText("Welcome"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 15) });
        
        Button linkBtn = new Button() { Content = GetText("GetLink"), Margin = new Thickness(0, 0, 0, 15), Padding = new Thickness(5), Cursor = Cursors.Hand };
        linkBtn.Click += (s, e) => Process.Start(new ProcessStartInfo("https://aistudio.google.com/app/apikey") { UseShellExecute = true });
        stack.Children.Add(linkBtn);
        
        TextBox input = new TextBox() { Margin = new Thickness(0, 0, 0, 15), Padding = new Thickness(5) };
        stack.Children.Add(input);
        
        Button saveBtn = new Button() { Content = GetText("SaveBtn"), Padding = new Thickness(5), IsDefault = true };
        saveBtn.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(input.Text))
            {
                SaveApiKey(input.Text); geminiApiKey = input.Text.Trim(); prompt.Close();
                AiResponseScroll.Visibility = Visibility.Visible; ExpandWindow();
                AiResponseText.Text = GetText("KeySaved");
            }
        };
        stack.Children.Add(saveBtn);
        prompt.Content = stack;
        prompt.ShowDialog();
    }

    private async Task AskGemini(string userText)
    {
        // 1. Vágólap tartalmának kinyerése (Itt hozzuk létre a változót!)
        string clipboardText = "";
        try { if (Clipboard.ContainsText()) clipboardText = Clipboard.GetText(); } catch { } 

        // 2. Nyelvi instrukció beállítása
        string conversationalInstruction = "";
        switch (AppLang)
        {
            case "en": conversationalInstruction = "CRITICAL INSTRUCTION: You MUST answer the user's question ENTIRELY in English, going into detail!"; break;
            case "de": conversationalInstruction = "WICHTIGE ANWEISUNG: Du MUSST die Frage des Benutzers VOLLSTÄNDIG auf Deutsch beantworten!"; break;
            case "fr": conversationalInstruction = "INSTRUCTION CRITIQUE : Vous DEVEZ répondre à la question de l'utilisateur ENTIÈREMENT en Français !"; break;
            default: conversationalInstruction = "FONTOS UTASÍTÁS: A kérdésre részletesen, kizárólag MAGYARUL válaszolj!"; break;
        }

        // 3. A prompt összerakása (Figyelj a $@ jelekre az elején!)
        string prompt = $@"Te 'Gemini' vagy, egy okos asszisztens. Kétféleképpen működsz:

1. GÉPVEZÉRLÉS: CSAK szögletes zárójeles parancs, magyarázat nélkül!
- Nyitás ➔ [OPEN:X] (pl. [OPEN:Chrome])
- Zárás ➔ [CLOSE:Y] (pl. [CLOSE:Firefox])
- Mappa/Fájl nyitás ➔ [OPENFOLDER:Z], [OPENFILE:F]
- Web/Keresés ➔ [URL:U], [SEARCH:W]
- Rendszer ➔ [CMD:MUTE], [CMD:SLEEP], [CMD:RESTART], [CMD:SHUTDOWN], [CMD:CLOSEALL]
- Egyéb ➔ [CALC:5+5], [TIMER:másodperc:üzenet], [NOTE:szöveg], [SYSINFO]

2. BESZÉLGETÉS:
Ha nem gépet vezérel, FELEJTSD EL a parancsokat! Helyette {PromptLangName} válaszolj részletesen a kérdésre!
{conversationalInstruction}

Vágólap: '{clipboardText}'
Felhasználó kérése: {userText}
Válasz:";

        try
        {
            string listUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key={geminiApiKey}";
            var listResponse = await httpClient.GetAsync(listUrl);
            if (!listResponse.IsSuccessStatusCode) { AiResponseText.Text = "API Error"; return; }
            string listJson = await listResponse.Content.ReadAsStringAsync();
            List<string> potentialModels = new List<string>();
            using (JsonDocument listDoc = JsonDocument.Parse(listJson)) { if (listDoc.RootElement.TryGetProperty("models", out JsonElement modelsArray)) { foreach (var model in modelsArray.EnumerateArray()) { string modelName = model.GetProperty("name").GetString(); if (modelName.Contains("gemini")) potentialModels.Add(modelName); } } }
            if (potentialModels.Count == 0) return;

            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            string jsonBody = JsonSerializer.Serialize(requestBody);
            string responseString = "";
            bool success = false;
            foreach (string modelName in potentialModels)
            {
                using (var content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                {
                    var response = await httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/{modelName}:generateContent?key={geminiApiKey}", content);
                    responseString = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode) { success = true; break; }
                }
            }
            if (!success) { AiResponseText.Text = "API Error"; return; }

            using JsonDocument doc = JsonDocument.Parse(responseString);
            string aiResponse = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString().Trim();

            if (aiResponse.Contains("[OPEN:") && aiResponse.Contains("]")) { string a = aiResponse.Split("[OPEN:")[1].Split("]")[0].Trim(); AiResponseText.Text = $"{a} {GetText("Opening")}"; SearchAndLaunchProgram(a); }
            else if (aiResponse.Contains("[CLOSE:") && aiResponse.Contains("]")) { string a = aiResponse.Split("[CLOSE:")[1].Split("]")[0].Trim(); AiResponseText.Text = $"{a} {GetText("Closing")}"; CloseProgram(a); }
            else if (aiResponse.Contains("[SEARCH:") && aiResponse.Contains("]")) { Process.Start(new ProcessStartInfo("https://www.google.com/search?q=" + Uri.EscapeDataString(aiResponse.Split("[SEARCH:")[1].Split("]")[0].Trim())) { UseShellExecute = true }); AiResponseText.Text = GetText("Searching"); }
            else if (aiResponse.Contains("[URL:") && aiResponse.Contains("]")) { Process.Start(new ProcessStartInfo(aiResponse.Split("[URL:")[1].Split("]")[0].Trim()) { UseShellExecute = true }); AiResponseText.Text = GetText("WebOpened"); }
            else if (aiResponse.Contains("[CMD:")) 
            { 
                string cmd = aiResponse.Split("[CMD:")[1].Split("]")[0].Trim();
                if (cmd == "SHUTDOWN") Process.Start("shutdown", "/s /t 0");
                else if (cmd == "RESTART") Process.Start("shutdown", "/r /t 0");
                else if (cmd == "MUTE") SendMessageW(new WindowInteropHelper(this).Handle, WM_APPCOMMAND, new WindowInteropHelper(this).Handle, (IntPtr)APPCOMMAND_VOLUME_MUTE);
            }
            else { AiResponseText.Text = aiResponse; }
        }
        catch (Exception ex) { AiResponseText.Text = $"Error: {ex.Message}"; }
    }

    private void SearchAndLaunchProgram(string appName)
    {
        try
        {
            using (var connection = new OleDbConnection(@"Provider=Search.CollatorDSO;Extended Properties=""Application=Windows"""))
            {
                connection.Open();
                using (var command = new OleDbCommand($"SELECT TOP 1 System.ItemPathDisplay FROM SystemIndex WHERE System.FileName LIKE '%{appName}%' AND (System.ItemPathDisplay LIKE '%.exe' OR System.ItemPathDisplay LIKE '%.lnk')", connection))
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read()) { Process.Start(new ProcessStartInfo(reader[0].ToString()) { UseShellExecute = true }); ResetSearch(); return; }
                }
            }
            AiResponseText.Text = GetText("NotFound");
        } catch { }
    }

    private void CloseProgram(string appName)
    {
        try
        {
            string searchName = appName.ToLower();
            foreach (var process in Process.GetProcesses()) { try { if (process.ProcessName.ToLower().Contains(searchName) || process.MainWindowTitle.ToLower().Contains(searchName)) process.Kill(); } catch { } }
            AiResponseText.Text = $"{appName} {GetText("Closing")}";
        } catch { }
    }

    private void ExpandWindow() { var mainGrid = (Grid)VisualTreeHelper.GetChild(this.Content as DependencyObject, 0); mainGrid.BeginAnimation(HeightProperty, null); mainGrid.Height = 350; }
    private void ResetSearch() { SearchInput.Text = ""; SearchResultsList.ItemsSource = null; SearchResultsList.Visibility = Visibility.Collapsed; AiResponseScroll.Visibility = Visibility.Collapsed; SearchResultsList.SelectedItem = null; }
}