using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace DIP_Oct7;

public partial class MainWindow : Window
{
    private static readonly FontFamily SansFontFamily = new("Inter, -apple-system, 'Segoe UI', Roboto, sans-serif");
    private static readonly FontFamily SerifFontFamily = new("'Playfair Display', 'Libre Baskerville', Georgia, 'Times New Roman', serif");

    private WriteableBitmap? _currentBitmap;
    private CoinDetectionResult? _detectionResult;
    private CoinDenomination? _activeFilter;
    private bool _showNthOnly;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GetImage.jpeg");
        if (!File.Exists(defaultPath))
        {
            defaultPath = "/Users/mccauleybacalla/RiderProjects/DIP_Oct7/DIP_Oct7/GetImage.jpeg";
        }

        if (File.Exists(defaultPath))
        {
            LoadImageFromPath(defaultPath);
        }
    }

    private async void BtnLoadImage_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Coin Image",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Image Files")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp" }
                    }
                }
            });

            if (files.Count > 0)
            {
                string localPath = files[0].Path.LocalPath;
                LoadImageFromPath(localPath);
            }
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Error loading image: {ex.Message}";
        }
    }

    private void LoadImageFromPath(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            _currentBitmap = WriteableBitmap.Decode(stream);

            ImgDisplay.Source = _currentBitmap;
            int w = _currentBitmap.PixelSize.Width;
            int h = _currentBitmap.PixelSize.Height;

            ImgDisplay.Width = w;
            ImgDisplay.Height = h;
            OverlayCanvas.Width = w;
            OverlayCanvas.Height = h;

            _detectionResult = null;
            _activeFilter = null;
            _showNthOnly = false;
            OverlayCanvas.Children.Clear();

            ResetSummaryCards();
            TxtStatus.Text = $"Loaded '{Path.GetFileName(filePath)}' ({w}x{h} px). Click 'Count the total denomination of the coins' to process.";
            TxtActiveMode.Text = "Mode: Image Loaded";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Failed to load image: {ex.Message}";
        }
    }

    private void EnsureDetection()
    {
        if (_detectionResult == null && _currentBitmap != null)
        {
            TxtStatus.Text = "Running image processing analysis...";
            var watch = System.Diagnostics.Stopwatch.StartNew();
            _detectionResult = CoinDetector.Process(_currentBitmap);
            watch.Stop();

            UpdateSummaryDisplay(_detectionResult);
            TxtStatus.Text = $"Processed {_detectionResult.TotalCoins} coins in {watch.ElapsedMilliseconds} ms. Total: PHP {_detectionResult.TotalValue:F2}.";
        }
    }

    private void BtnCountCoins_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentBitmap == null)
        {
            TxtStatus.Text = "Please load an image first.";
            return;
        }

        EnsureDetection();
        if (_detectionResult == null) return;

        _activeFilter = null;
        _showNthOnly = false;
        TxtActiveMode.Text = "Mode: All Coins (Denomination View)";
        RenderOverlay();
    }

    private void BtnShowRectangles_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentBitmap == null)
        {
            TxtStatus.Text = "Please load an image first.";
            return;
        }

        EnsureDetection();
        if (_detectionResult == null) return;

        _activeFilter = null;
        _showNthOnly = true;
        TxtActiveMode.Text = "Mode: Nth Count (Top-Left to Bottom-Right)";
        TxtStatus.Text = $"Showing {_detectionResult.TotalCoins} coins ordered from top-left to bottom-right (indices 1 to {_detectionResult.TotalCoins}).";
        RenderOverlay();
    }

    private void BtnShowAll_Click(object? sender, RoutedEventArgs e)
    {
        if (_detectionResult == null)
        {
            BtnCountCoins_Click(sender, e);
            return;
        }

        _activeFilter = null;
        _showNthOnly = false;
        TxtActiveMode.Text = "Mode: All Coins";
        RenderOverlay();
    }

    private void Btn5Centavos_Click(object? sender, RoutedEventArgs e) => FilterByDenomination(CoinDenomination.Centavo5, "5 cents");
    private void Btn10Centavos_Click(object? sender, RoutedEventArgs e) => FilterByDenomination(CoinDenomination.Centavo10, "10 cents");
    private void Btn25Centavos_Click(object? sender, RoutedEventArgs e) => FilterByDenomination(CoinDenomination.Centavo25, "25 cents");
    private void Btn1Peso_Click(object? sender, RoutedEventArgs e) => FilterByDenomination(CoinDenomination.Peso1, "1 peso");
    private void Btn5Pesos_Click(object? sender, RoutedEventArgs e) => FilterByDenomination(CoinDenomination.Peso5, "5 pesos");

    private void FilterByDenomination(CoinDenomination denomination, string name)
    {
        if (_currentBitmap == null)
        {
            TxtStatus.Text = "Please load an image first.";
            return;
        }

        EnsureDetection();
        if (_detectionResult == null) return;

        _activeFilter = denomination;
        _showNthOnly = false;

        int count = _detectionResult.Coins.Count(c => c.Denomination == denomination);
        double totalVal = count * CoinDetector.GetDenominationValue(denomination);

        TxtActiveMode.Text = $"Filter: {name}";
        TxtStatus.Text = $"Filtered {name}: {count} coins identified (Subtotal: PHP {totalVal:F2}).";
        RenderOverlay();
    }

    private void UpdateSummaryDisplay(CoinDetectionResult res)
    {
        TxtCount5Centavos.Text = $"{res.Count5Centavos} coins";
        TxtValue5Centavos.Text = $"PHP {res.Count5Centavos * 0.05:F2}";

        TxtCount10Centavos.Text = $"{res.Count10Centavos} coins";
        TxtValue10Centavos.Text = $"PHP {res.Count10Centavos * 0.10:F2}";

        TxtCount25Centavos.Text = $"{res.Count25Centavos} coins";
        TxtValue25Centavos.Text = $"PHP {res.Count25Centavos * 0.25:F2}";

        TxtCount1Peso.Text = $"{res.Count1Peso} coins";
        TxtValue1Peso.Text = $"PHP {res.Count1Peso * 1.00:F2}";

        TxtCount5Pesos.Text = $"{res.Count5Pesos} coins";
        TxtValue5Pesos.Text = $"PHP {res.Count5Pesos * 5.00:F2}";

        TxtTotalAmount.Text = $"PHP {res.TotalValue:F2}";
        TxtTotalCount.Text = $"({res.TotalCoins} coins detected)";
    }

    private void ResetSummaryCards()
    {
        TxtCount5Centavos.Text = "0 coins";
        TxtValue5Centavos.Text = "PHP 0.00";
        TxtCount10Centavos.Text = "0 coins";
        TxtValue10Centavos.Text = "PHP 0.00";
        TxtCount25Centavos.Text = "0 coins";
        TxtValue25Centavos.Text = "PHP 0.00";
        TxtCount1Peso.Text = "0 coins";
        TxtValue1Peso.Text = "PHP 0.00";
        TxtCount5Pesos.Text = "0 coins";
        TxtValue5Pesos.Text = "PHP 0.00";
        TxtTotalAmount.Text = "PHP 0.00";
        TxtTotalCount.Text = "(0 coins detected)";
    }

    private void RenderOverlay()
    {
        OverlayCanvas.Children.Clear();
        if (_detectionResult == null) return;

        var coinsToDisplay = _detectionResult.Coins;
        if (_activeFilter.HasValue)
        {
            coinsToDisplay = coinsToDisplay.Where(c => c.Denomination == _activeFilter.Value).ToList();
        }

        // Muted color palette: restrained navy accents and neutral steel tones
        var strokeBrush = new SolidColorBrush(Color.Parse("#5B728D"));
        var fillBrush = new SolidColorBrush(Color.FromArgb(28, 64, 85, 112));
        var badgeBgBrush = new SolidColorBrush(Color.Parse("#263140"));
        var badgeTextBrush = new SolidColorBrush(Color.Parse("#EDEFF2"));

        foreach (var coin in coinsToDisplay)
        {
            // 1. Rectangular boundary box
            var rectBorder = new Border
            {
                BorderBrush = strokeBrush,
                BorderThickness = new Thickness(1.5),
                Background = fillBrush,
                Width = coin.Width + 6,
                Height = coin.Height + 6,
                CornerRadius = new CornerRadius(2),
                IsHitTestVisible = false,
                [Canvas.LeftProperty] = (double)(coin.MinX - 3),
                [Canvas.TopProperty] = (double)(coin.MinY - 3)
            };
            OverlayCanvas.Children.Add(rectBorder);

            // 2. Nth count or denomination label badge
            string labelText = _showNthOnly
                ? $"#{coin.Index}"
                : $"{coin.DisplayName} (#{coin.Index})";

            double badgeTop = coin.MinY - 18;
            if (badgeTop < 0)
            {
                badgeTop = coin.MinY + 3;
            }

            var badge = new Border
            {
                Background = badgeBgBrush,
                BorderBrush = strokeBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(4, 1),
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = labelText,
                    FontFamily = SansFontFamily,
                    Foreground = badgeTextBrush,
                    FontSize = 10,
                    FontWeight = FontWeight.Medium
                },
                [Canvas.LeftProperty] = (double)coin.MinX,
                [Canvas.TopProperty] = badgeTop
            };
            OverlayCanvas.Children.Add(badge);
        }
    }
}