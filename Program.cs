using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

internal static class Program
{
    const string MusicUrl = "https://www.youtube.com/watch?v=2cMhAm-GgX4&t=50s&autoplay=1";
    static int W, H;
    static Cell[] frame;
    static bool quit;
    static IntPtr output;
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    struct Cell { [FieldOffset(0)] public char Ch; [FieldOffset(2)] public short Color; }
    [StructLayout(LayoutKind.Sequential)]
    struct Coord { public short X, Y; public Coord(int x, int y) { X = (short)x; Y = (short)y; } }
    [StructLayout(LayoutKind.Sequential)]
    struct Rect { public short Left, Top, Right, Bottom; }
    [DllImport("kernel32.dll")] static extern IntPtr GetStdHandle(int id);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool WriteConsoleOutputW(IntPtr h, Cell[] buffer, Coord size, Coord origin, ref Rect region);
    static MediaPlayer music;
    static string musicError;
    static bool musicErrorShown;

    // Console applications have no WPF message loop. Pump media events explicitly.
    static void PumpMedia()
    {
        DispatcherFrame pending = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
            new Action(delegate { pending.Continue = false; }));
        Dispatcher.PushFrame(pending);
    }
    static string StartMusic(string path)
    {
        bool opened = false;
        musicError = null;
        try
        {
            music = new MediaPlayer();
            music.Volume = 0.85;
            music.MediaOpened += delegate { opened = true; };
            music.MediaFailed += (sender, e) =>
            {
                musicError = e.ErrorException == null
                    ? "Ses dosyasi acilamadi."
                    : e.ErrorException.Message;
            };
            music.Open(new Uri(Path.GetFullPath(path), UriKind.Absolute));
            Stopwatch timeout = Stopwatch.StartNew();
            while (!opened && musicError == null && timeout.Elapsed.TotalSeconds < 15 && !quit)
            {
                PumpMedia(); Thread.Sleep(15);
            }
            if (quit) return "Islem iptal edildi.";
            if (musicError != null) return musicError;
            if (!opened) return "Ses dosyasi 15 saniye icinde acilamadi.";
            if (music.NaturalDuration.HasTimeSpan && music.NaturalDuration.TimeSpan.TotalSeconds <= 50)
                return "Dosya 50 saniyeden uzun olmali; baslangic noktasi 00:50.";
            music.Position = TimeSpan.FromSeconds(50);
            music.Play();
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }
    static void StopMusic()
    {
        if (music == null) return;
        try { music.Close(); } catch { }
        music = null;
    }

    static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]> {
        {'H',new[]{"10001","10001","10001","11111","10001","10001","10001"}},
        {'A',new[]{"01110","10001","10001","11111","10001","10001","10001"}},
        {'Y',new[]{"10001","10001","01010","00100","00100","00100","00100"}},
        {'I',new[]{"00100","00100","00100","00100","00100","00100","00100"}},
        {'R',new[]{"11110","10001","10001","11110","10100","10010","10001"}},
        {'L',new[]{"10000","10000","10000","10000","10000","10000","11111"}},
        {'C',new[]{"01111","10000","10000","10000","10000","10000","01111"}},
        {'U',new[]{"10001","10001","10001","10001","10001","10001","01110"}},
        {'M',new[]{"10001","11011","10101","10101","10001","10001","10001"}}
    };

    [STAThread]
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            Console.WriteLine("Bu uygulama Windows CMD icin hazirlanmistir."); return;
        }
        if (Console.IsOutputRedirected || Console.IsInputRedirected)
        {
            Console.WriteLine("Uygulamayi CMD penceresinde acin."); return;
        }
        Console.Title = "CUMA.EXE | Hayırlı Cumalar";
        Console.WriteLine("CUMA.EXE - Guller hazir, cuma modu beklemede!\n");
        Console.WriteLine("ENTER: Muzigi ac / hazirla   S: Sessiz baslat   ESC: Cik");
        Console.WriteLine("YouTube acilirsa oynatip CMD'ye donun; ENTER ile animasyonu baslatin.");
        Console.WriteLine("Alternatif: Bu klasore ilahi.mp3 koyun; 50. saniyeden otomatik calar.");
        ConsoleKey key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.Escape) return;
        bool silent = key == ConsoleKey.S;
        string mp3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ilahi.mp3");
        if (!silent && !File.Exists(mp3))
        {
            try { Process.Start(new ProcessStartInfo(MusicUrl) { UseShellExecute = true }); }
            catch (Exception ex) { Console.WriteLine("Tarayici acilamadi: " + ex.Message + "\n" + MusicUrl); }
            Console.WriteLine("Muzik hazirsa ENTER. (YouTube sesi tarayicidan kontrol edilir.)");
            while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
        }
        Console.CancelKeyPress += delegate (object s, ConsoleCancelEventArgs e) { e.Cancel = true; quit = true; };
        bool cursor = Console.CursorVisible;
        try
        {
            if (!silent && File.Exists(mp3))
            {
                Console.WriteLine("MP3 hazirlaniyor (00:50)...");
                string error = StartMusic(mp3);
                if (error != null)
                {
                    StopMusic();
                    Console.WriteLine("Ses acilamadi: " + error);
                    Console.WriteLine("Dosya: " + mp3);
                    Console.WriteLine("Dosyayi Windows oynaticisinda acmayi deneyin. Sessiz devam: ENTER");
                    Console.ReadLine();
                }
            }
            try { Console.SetWindowSize(Math.Min(140, Console.LargestWindowWidth), Math.Min(48, Console.LargestWindowHeight)); } catch { }
            Console.Clear(); Console.CursorVisible = false;
            output = GetStdHandle(-11);
            Stopwatch clock = Stopwatch.StartNew();
            while (!quit)
            {
                PumpMedia();
                if (music != null && musicError != null && !musicErrorShown)
                {
                    musicErrorShown = true;
                    Console.Clear();
                    Console.WriteLine("Ses oynatma hatasi: " + musicError + "\nSessiz devam: ENTER");
                    StopMusic(); Console.ReadLine(); Console.Clear();
                }
                while (Console.KeyAvailable)
                {
                    key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Escape) quit = true;
                    if (key == ConsoleKey.R) clock.Restart();
                }
                W = Math.Max(1, Console.WindowWidth); H = Math.Max(1, Console.WindowHeight);
                frame = new Cell[W * H];
                for (int i = 0; i < frame.Length; i++) { frame[i].Ch = ' '; frame[i].Color = 7; }
                if (W < 72 || H < 30) Center("Pencereyi en az 72 x 30 yapin. ESC: cikis", H / 2, 14);
                else Draw(clock.Elapsed.TotalSeconds);
                Rect region = new Rect
                {
                    Left = (short)Console.WindowLeft,
                    Top = (short)Console.WindowTop,
                    Right = (short)(Console.WindowLeft + W - 1),
                    Bottom = (short)(Console.WindowTop + H - 1)
                };
                if (!WriteConsoleOutputW(output, frame, new Coord(W, H), new Coord(0, 0), ref region))
                    throw new IOException("Konsol cizimi basarisiz. Klasik CMD ile tekrar deneyin.");
                Thread.Sleep(33);
            }
        }
        catch (Exception ex) { Console.Clear(); Console.WriteLine(ex.Message); }
        finally
        {
            StopMusic();
            Console.CursorVisible = cursor; Console.ResetColor(); Console.Clear();
            Console.WriteLine("Hayırlı Cumalar! YouTube açıksa müziği tarayıcıdan durdurabilirsiniz.");
        }
    }
    static void Put(int x, int y, char c, int color)
    {
        if (x >= 0 && x < W && y >= 0 && y < H) frame[y * W + x] = new Cell { Ch = c, Color = (short)color };
    }
    static void Text(string text, int x, int y, int color)
    {
        for (int i = 0; i < text.Length; i++) Put(x + i, y, text[i], color);
    }
    static void Center(string text, int y, int color) { Text(text, (W - text.Length) / 2, y, color); }
    static double Ease(double t) { t = Math.Max(0, Math.Min(1, t)); return 1 - Math.Pow(1 - t, 3); }
    static void Rose(int x, int y, double t)
    {
        string[] bloom = { "   .-.-.   ", "  ( @ @ )  ", "   \\ V /   ", "    `-'    " };
        x += (int)Math.Round(Math.Sin(t * 1.4 + x) * 1.2);
        for (int j = 0; j < bloom.Length; j++) Text(bloom[j], x, y + j, j == 1 ? 13 : 12);
        Text("     |", x, y + 4, 10); Text("  <\\ | / >", x, y + 5, 2);
        Text("    \\|/", x, y + 6, 10); Text("     |", x, y + 7, 2);
    }
    static void Big(string word, double cy, double scale, double slide, int color)
    {
        int bw = word.Length * 6 - 1;
        int dw = Math.Max(1, (int)Math.Round(bw * scale)), dh = Math.Max(1, (int)Math.Round(7 * scale));
        int left = (W - dw) / 2 + (int)slide, top = (int)cy - dh / 2;
        for (int y = 0; y < dh; y++) for (int x = 0; x < dw; x++)
            {
                // Map the rounded destination size to BOTH endpoints of the glyph.
                // Dividing by scale could omit the last row (the foot of L).
                int sx = dw <= 1 ? 0 : (int)Math.Round(x * (bw - 1.0) / (dw - 1));
                int sy = dh <= 1 ? 0 : (int)Math.Round(y * 6.0 / (dh - 1));
                int letter = sx / 6, col = sx % 6;
                if (col < 5 && letter < word.Length && Font[word[letter]][sy][col] == '1') Put(left + x, top + y, '█', color);
            }
    }
    static void Draw(double t)
    {
        Center("C:\\GONUL> cuma.exe --guller --huzur", 1, 10);
        Center(t < 3 ? "[ OK ] Cuma modu yukleniyor..." : "[ OK ] Cuma modu yuklendi. Dertler karantinaya alindi.", 3, 8);
        for (int i = 0; i < 5; i++) Rose((i + 1) * W / 6 - 5, 5, t);
        // Deterministic falling sparkles: no random flickering between frames.
        for (int i = 0; i < 18; i++)
        {
            int x = (i * 37 + 11) % W, y = 4 + (int)((t * (1 + i % 3) + i * 7) % Math.Max(1, H - 8));
            if (x < W / 7 || x > W * 6 / 7) Put(x, y, i % 3 == 0 ? '♥' : '.', i % 2 == 0 ? 12 : 8);
        }
        double ready = Math.Max(0, t - 6), phase = ready % 1.4;
        double beat = Math.Exp(-Math.Pow((phase - .12) / .09, 2)) + .55 * Math.Exp(-Math.Pow((phase - .36) / .12, 2));
        double maxScale = Math.Min((W - 8) / 41.0, (H - 18) / 17.0);
        double scale = Math.Min(1.35, maxScale) * (1 + .13 * beat);
        scale = Math.Min(scale, maxScale);
        double y1 = H * .43 + 1, y2 = y1 + 8 * scale + 2;
        double rise1 = (H + 10 - y1) * (1 - Ease((t - 1.5) / 2.5));
        double rise2 = (H + 18 - y2) * (1 - Ease((t - 2.4) / 2.5));
        Big("HAYIRLI", y1 + rise1, scale, -W * (1 - Ease((t - 1.5) / 2.5)), 14);
        Big("CUMALAR", y2 + rise2, scale, W * (1 - Ease((t - 2.4) / 2.5)), ready > 0 && beat > .5 ? 13 : 12);
        if (t > 6) Center("♥  Hayırlı Cumalar  ♥", H - 5, 13);
        Center("Bugun bug yok, huzur var :)", H - 3, 10);
        Center("R: animasyonu tekrarla   |   ESC: cikis", H - 1, 8);
    }
}
