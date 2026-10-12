using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;

namespace DIP_Oct7;

public enum CoinDenomination
{
    Centavo5,
    Centavo10,
    Centavo25,
    Peso1,
    Peso5
}

public record CoinInfo(
    int Index,               // N-th count from top-left to bottom-right (1-based)
    int MinX,
    int MinY,
    int MaxX,
    int MaxY,
    int Width,
    int Height,
    double CenterX,
    double CenterY,
    int Area,
    double EquivRadius,
    bool HasHole,
    CoinDenomination Denomination)
{
    public double Value => CoinDetector.GetDenominationValue(Denomination);
    public string DisplayName => CoinDetector.GetDenominationDisplayName(Denomination);
}

public record CoinDetectionResult(
    List<CoinInfo> Coins,
    int Count5Centavos,
    int Count10Centavos,
    int Count25Centavos,
    int Count1Peso,
    int Count5Pesos,
    double TotalValue)
{
    public int TotalCoins => Coins.Count;
}

/// <summary>
/// Digital Image Processing (DIP) Engine implemented in pure C#.
/// Does NOT use OpenCV or any external computer vision libraries.
/// Replicates core DIP algorithms:
///   1. Grayscale Conversion (Luminance ITU-R BT.601)
///   2. Adaptive / Otsu Binarization (thresholding)
///   3. Connected Component Labeling via Breadth-First Search (BFS)
///   4. Spatial Moment Analysis (Centroid, Area, Equivalent Radius)
///   5. Morphological Hole Detection (detecting center hole in 5-centimo coins)
///   6. 2D Topological Row Sorting (Top-Left to Bottom-Right reading order)
///   7. Physical Dimension-based Denomination Classification
/// </summary>
public static class CoinDetector
{
    public static double GetDenominationValue(CoinDenomination d) => d switch
    {
        CoinDenomination.Centavo5  => 0.05,
        CoinDenomination.Centavo10 => 0.10,
        CoinDenomination.Centavo25 => 0.25,
        CoinDenomination.Peso1     => 1.00,
        CoinDenomination.Peso5     => 5.00,
        _                          => 0.00
    };

    public static string GetDenominationDisplayName(CoinDenomination d) => d switch
    {
        CoinDenomination.Centavo5  => "5c",
        CoinDenomination.Centavo10 => "10c",
        CoinDenomination.Centavo25 => "25c",
        CoinDenomination.Peso1     => "1 Peso",
        CoinDenomination.Peso5     => "5 Pesos",
        _                          => "?"
    };

    public static CoinDetectionResult Process(WriteableBitmap bitmap)
    {
        using var fb = bitmap.Lock();
        int width = fb.Size.Width;
        int height = fb.Size.Height;
        int stride = fb.RowBytes;

        // Copy raw pixel buffer
        byte[] pixels = new byte[stride * height];
        System.Runtime.InteropServices.Marshal.Copy(fb.Address, pixels, 0, pixels.Length);

        // Step 1: Grayscale conversion
        byte[] gray = new byte[width * height];
        int[] hist = new int[256];

        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * stride;
            int grayOffset = y * width;
            for (int x = 0; x < width; x++)
            {
                int pIdx = rowOffset + x * 4;
                // Standard 32-bit RGBA or BGRA format
                byte b0 = pixels[pIdx + 0];
                byte b1 = pixels[pIdx + 1];
                byte b2 = pixels[pIdx + 2];
                // ITU-R BT.601 luminance
                byte gVal = (byte)(0.299 * b2 + 0.587 * b1 + 0.114 * b0);
                gray[grayOffset + x] = gVal;
                hist[gVal]++;
            }
        }

        // Step 2: Otsu's Thresholding algorithm
        int threshold = CalculateOtsuThreshold(hist, width * height);
        // Ensure threshold separates white background (>200) from coins (<190)
        if (threshold > 200) threshold = 195;
        if (threshold < 160) threshold = 185;

        // Step 3: Binarization (Foreground = dark coins, Background = light paper)
        bool[,] binary = new bool[width, height];
        for (int y = 0; y < height; y++)
        {
            int grayOffset = y * width;
            for (int x = 0; x < width; x++)
            {
                binary[x, y] = gray[grayOffset + x] < threshold;
            }
        }

        // Step 4: Connected Component Analysis using BFS
        bool[,] visited = new bool[width, height];
        var rawComponents = new List<(double X, double Y, int minX, int minY, int maxX, int maxY, int area, double r, bool hole)>();

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (binary[x, y] && !visited[x, y])
                {
                    int area = 0;
                    long sumX = 0, sumY = 0;
                    int minX = x, maxX = x, minY = y, maxY = y;

                    var queue = new Queue<(int, int)>();
                    queue.Enqueue((x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();
                        area++;
                        sumX += cx;
                        sumY += cy;

                        if (cx < minX) minX = cx;
                        if (cx > maxX) maxX = cx;
                        if (cy < minY) minY = cy;
                        if (cy > maxY) maxY = cy;

                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cx + dx[d];
                            int ny = cy + dy[d];
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height && binary[nx, ny] && !visited[nx, ny])
                            {
                                visited[nx, ny] = true;
                                queue.Enqueue((nx, ny));
                            }
                        }
                    }

                    // Ignore small noise specks (< 500 pixels)
                    if (area > 500)
                    {
                        double cx = (double)sumX / area;
                        double cy = (double)sumY / area;
                        double equivRadius = Math.Sqrt(area / Math.PI);

                        // Morphological Central Hole Detection (for 5 centavos coins)
                        int icx = (int)Math.Round(cx);
                        int icy = (int)Math.Round(cy);
                        int holeCheckRadius = 6;
                        int sampleCount = 0;
                        int brightCount = 0;

                        for (int hy = -holeCheckRadius; hy <= holeCheckRadius; hy++)
                        {
                            for (int hx = -holeCheckRadius; hx <= holeCheckRadius; hx++)
                            {
                                if (hx * hx + hy * hy <= holeCheckRadius * holeCheckRadius)
                                {
                                    int px = icx + hx;
                                    int py = icy + hy;
                                    if (px >= 0 && px < width && py >= 0 && py < height)
                                    {
                                        sampleCount++;
                                        if (gray[py * width + px] > 180)
                                        {
                                            brightCount++;
                                        }
                                    }
                                }
                            }
                        }
                        bool hasHole = sampleCount > 0 && ((double)brightCount / sampleCount > 0.55);

                        rawComponents.Add((cx, cy, minX, minY, maxX, maxY, area, equivRadius, hasHole));
                    }
                }
            }
        }

        // Step 5: Topological Row-based Sorting (Top-Left to Bottom-Right)
        var sortedByY = rawComponents.OrderBy(c => c.Y).ToList();
        var rows = new List<List<(double X, double Y, int minX, int minY, int maxX, int maxY, int area, double r, bool hole)>>();
        const double rowTolerance = 45.0; // max delta Y between coins on same row

        foreach (var c in sortedByY)
        {
            bool placed = false;
            foreach (var row in rows)
            {
                double avgY = row.Average(item => item.Y);
                if (Math.Abs(c.Y - avgY) < rowTolerance)
                {
                    row.Add(c);
                    placed = true;
                    break;
                }
            }
            if (!placed)
            {
                rows.Add(new List<(double X, double Y, int minX, int minY, int maxX, int maxY, int area, double r, bool hole)> { c });
            }
        }

        // Re-order rows strictly by their mean Y, then sort each row by X ascending
        rows.Sort((r1, r2) => r1.Average(item => item.Y).CompareTo(r2.Average(item => item.Y)));

        var finalCoins = new List<CoinInfo>();
        int nthIndex = 1;

        foreach (var row in rows)
        {
            var orderedInRow = row.OrderBy(c => c.X).ToList();
            foreach (var c in orderedInRow)
            {
                var denom = Classify(c.r, c.hole);
                finalCoins.Add(new CoinInfo(
                    nthIndex++,
                    c.minX,
                    c.minY,
                    c.maxX,
                    c.maxY,
                    c.maxX - c.minX + 1,
                    c.maxY - c.minY + 1,
                    c.X,
                    c.Y,
                    c.area,
                    c.r,
                    c.hole,
                    denom
                ));
            }
        }

        // Aggregate statistics
        int c5 = finalCoins.Count(c => c.Denomination == CoinDenomination.Centavo5);
        int c10 = finalCoins.Count(c => c.Denomination == CoinDenomination.Centavo10);
        int c25 = finalCoins.Count(c => c.Denomination == CoinDenomination.Centavo25);
        int p1 = finalCoins.Count(c => c.Denomination == CoinDenomination.Peso1);
        int p5 = finalCoins.Count(c => c.Denomination == CoinDenomination.Peso5);

        double totalVal = c5 * 0.05 + c10 * 0.10 + c25 * 0.25 + p1 * 1.00 + p5 * 5.00;

        return new CoinDetectionResult(finalCoins, c5, c10, c25, p1, p5, totalVal);
    }

    /// <summary>
    /// Replicated Otsu's thresholding algorithm for optimal binarization.
    /// </summary>
    private static int CalculateOtsuThreshold(int[] histogram, int totalPixels)
    {
        double sum = 0;
        for (int i = 0; i < 256; i++)
            sum += i * histogram[i];

        double sumB = 0;
        int wB = 0;
        double maxVariance = 0;
        int bestThreshold = 180;

        for (int t = 0; t < 256; t++)
        {
            wB += histogram[t];
            if (wB == 0) continue;
            int wF = totalPixels - wB;
            if (wF == 0) break;

            sumB += t * histogram[t];
            double mB = sumB / wB;
            double mF = (sum - sumB) / wF;

            double betweenVariance = (double)wB * wF * (mB - mF) * (mB - mF);
            if (betweenVariance > maxVariance)
            {
                maxVariance = betweenVariance;
                bestThreshold = t;
            }
        }

        return bestThreshold;
    }

    /// <summary>
    /// Classifies coin denomination based on equivalent circle radius and central hole.
    /// Philippine Peso Coin Diameters & equivalent radii:
    ///   5¢  : Holed coin, R ≈ 29.5 px
    ///   10¢ : R ≈ 32.7 px (solid, 16.7 mm)
    ///   25¢ : R ≈ 38.5 px (solid, 19.5 mm)
    ///   ₱1  : R ≈ 45.4 px (solid, 23.0 mm)
    ///   ₱5  : R ≈ 50.8 px (solid, 27.0 mm)
    /// </summary>
    private static CoinDenomination Classify(double equivRadius, bool hasHole)
    {
        if (hasHole || equivRadius < 31.0)
            return CoinDenomination.Centavo5;

        if (equivRadius < 35.5)
            return CoinDenomination.Centavo10;

        if (equivRadius < 42.0)
            return CoinDenomination.Centavo25;

        if (equivRadius < 48.0)
            return CoinDenomination.Peso1;

        return CoinDenomination.Peso5;
    }
}
