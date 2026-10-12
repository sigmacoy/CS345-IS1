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
    private WriteableBitmap? _currentBitmap;
    private CoinDetectionResult? _detectionResult;
    private CoinDenomination? _activeFilter;
    private bool _showNthOnly;

    public MainWindow()
    {
        InitializeComponent();

        // Automatically load default image GetImage.jpeg if present
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
            TxtStatus.Text = $"Loaded image '{Path.GetFileName(filePath)}' ({w}x{h}). Ready to count coins.";
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
            TxtStatus.Text = "Processing image using pure DIP algorithms...";
            var watch = System.Diagnostics.Stopwatch.StartNew();
            _detectionResult = CoinDetector.Process(_currentBitmap);
            watch.Stop();

            UpdateSummaryDisplay(_detectionResult);
            TxtStatus.Text = $"Detected {_detectionResult.TotalCoins} coins in {watch.ElapsedMilliseconds} ms. Total: ₱{_detectionResult.TotalValue:F2}";
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
        TxtActiveMode.Text = "Mode: All Coins with Denomination";
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
        TxtStatus.Text = $"Showing {_detectionResult.TotalCoins} coins sorted top-left to bottom-right (indices 1 to {_detectionResult.TotalCoins}).";
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

    private void Btn5Centavos_Click(object? sender, RoutedEventArgs e) => FilterByDenomination(CoinDenomination.Centavo5, "5 cents (5¢)");
    private void Btn10Centavos_Click(object? sender, RoutedEventArgs e) => FilterByDenomination(CoinDenomination.Centavo10, "10 cents (10¢)");
    private void Btn25Centavos_Click(object? sender, RoutedEventArgs e) => FilterByDenomination(CoinDenomination.Centavo25, "25 cents (25¢)");
    private void Btn1Peso_Click(object? sender, RoutedEventArgs e) => FilterByDenomination(CoinDenomination.Peso1, "1 peso (₱1)");
    private void Btn5Pesos_Click(object? sender, RoutedEventArgs e) => FilterByDenomination(CoinDenomination.Peso5, "5 pesos (₱5)");

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
        TxtStatus.Text = $"Filtered {name}: {count} coins found (Total: ₱{totalVal:F2}).";
        RenderOverlay();
    }

    private void UpdateSummaryDisplay(CoinDetectionResult res)
    {
        TxtCount5Centavos.Text = $"{res.Count5Centavos} coins";
        TxtValue5Centavos.Text = $"₱ {res.Count5Centavos * 0.05:F2}";

        TxtCount10Centavos.Text = $"{res.Count10Centavos} coins";
        TxtValue10Centavos.Text = $"₱ {res.Count10Centavos * 0.10:F2}";

        TxtCount25Centavos.Text = $"{res.Count25Centavos} coins";
        TxtValue25Centavos.Text = $"₱ {res.Count25Centavos * 0.25:F2}";

        TxtCount1Peso.Text = $"{res.Count1Peso} coins";
        TxtValue1Peso.Text = $"₱ {res.Count1Peso * 1.00:F2}";

        TxtCount5Pesos.Text = $"{res.Count5Pesos} coins";
        TxtValue5Pesos.Text = $"₱ {res.Count5Pesos * 5.00:F2}";

        TxtTotalAmount.Text = $"₱ {res.TotalValue:F2}";
        TxtTotalCount.Text = $"({res.TotalCoins} coins detected)";
    }

    private void ResetSummaryCards()
    {
        TxtCount5Centavos.Text = "0 coins";
        TxtValue5Centavos.Text = "₱ 0.00";
        TxtCount10Centavos.Text = "0 coins";
        TxtValue10Centavos.Text = "₱ 0.00";
        TxtCount25Centavos.Text = "0 coins";
        TxtValue25Centavos.Text = "₱ 0.00";
        TxtCount1Peso.Text = "0 coins";
        TxtValue1Peso.Text = "₱ 0.00";
        TxtCount5Pesos.Text = "0 coins";
        TxtValue5Pesos.Text = "₱ 0.00";
        TxtTotalAmount.Text = "₱ 0.00";
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

        foreach (var coin in coinsToDisplay)
        {
            var strokeBrush = GetBrushForDenomination(coin.Denomination);
            var fillBrush = GetFillBrushForDenomination(coin.Denomination);

            // 1. Rectangular boundary around the coin
            var rectBorder = new Border
            {
                BorderBrush = strokeBrush,
                BorderThickness = new Thickness(2),
                Background = fillBrush,
                Width = coin.Width + 6,
                Height = coin.Height + 6,
                CornerRadius = new CornerRadius(3),
                IsHitTestVisible = false,
                [Canvas.LeftProperty] = (double)(coin.MinX - 3),
                [Canvas.TopProperty] = (double)(coin.MinY - 3)
            };
            OverlayCanvas.Children.Add(rectBorder);

            // 2. Nth count / Denomination label badge
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
                Background = strokeBrush,
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1),
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = labelText,
                    Foreground = Brushes.White,
                    FontSize = 10,
                    FontWeight = FontWeight.Bold
                },
                [Canvas.LeftProperty] = (double)coin.MinX,
                [Canvas.TopProperty] = badgeTop
            };
            OverlayCanvas.Children.Add(badge);
        }
    }

    private static IBrush GetBrushForDenomination(CoinDenomination d) => d switch
    {
        CoinDenomination.Centavo5  => new SolidColorBrush(Color.Parse("#E64980")),
        CoinDenomination.Centavo10 => new SolidColorBrush(Color.Parse("#F76707")),
        CoinDenomination.Centavo25 => new SolidColorBrush(Color.Parse("#AE3EC9")),
        CoinDenomination.Peso1     => new SolidColorBrush(Color.Parse("#1C7ED6")),
        CoinDenomination.Peso5     => new SolidColorBrush(Color.Parse("#2F9E44")),
        _                          => new SolidColorBrush(Colors.Yellow)
    };

    private static IBrush GetFillBrushForDenomination(CoinDenomination d) => d switch
    {
        CoinDenomination.Centavo5  => new SolidColorBrush(Color.FromArgb(40, 230, 73, 128)),
        CoinDenomination.Centavo10 => new SolidColorBrush(Color.FromArgb(40, 247, 103, 7)),
        CoinDenomination.Centavo25 => new SolidColorBrush(Color.FromArgb(40, 174, 62, 201)),
        CoinDenomination.Peso1     => new SolidColorBrush(Color.FromArgb(40, 28, 126, 214)),
        CoinDenomination.Peso5     => new SolidColorBrush(Color.FromArgb(40, 47, 158, 68)),
        _                          => new SolidColorBrush(Color.FromArgb(30, 255, 255, 0))
    };
}