using System;
using System.Collections.Generic;
using System.Text;

namespace POS.Contract.Utilities;

/// <summary>
/// Robust, standard Code 128 (Code Set B) barcode generator without any external dependencies.
/// Generates SVG strings and module patterns readable by all 1D/2D optical barcode scanners.
/// </summary>
public static class Code128BarcodeGenerator
{
    // Code 128 patterns: 107 symbols (0 to 106).
    // Alternating bar and space widths (in modules).
    // Symbols 0-105 have 6 digits (sum=11). Stop symbol 106 has 7 digits (sum=13).
    private static readonly string[] Patterns = new string[]
    {
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213", // 0-9
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132", // 10-19
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211", // 20-29
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313", // 30-39
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331", // 40-49
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111", // 50-59
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214", // 60-69
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111", // 70-79
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141", // 80-89
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141", // 90-99
        "114131", "311141", "411131", "211412", "211214", "211232", "2331112"                                // 100-106
    };

    private const int StartCodeB = 104;
    private const int StopCode = 106;

    /// <summary>
    /// Encodes ASCII text into a sequence of Code 128 symbols (including Start B, Checksum, Stop).
    /// </summary>
    public static List<int> EncodeToSymbols(string text)
    {
        if (string.IsNullOrEmpty(text))
            text = "0";

        var symbols = new List<int> { StartCodeB };
        int checksum = StartCodeB;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            int charVal = (int)c - 32;
            if (charVal < 0 || charVal > 95)
                charVal = 0; // fallback to space

            symbols.Add(charVal);
            checksum += (i + 1) * charVal;
        }

        int checkDigit = checksum % 103;
        symbols.Add(checkDigit);
        symbols.Add(StopCode);

        return symbols;
    }

    /// <summary>
    /// Returns boolean modules (true for black bar, false for white space).
    /// </summary>
    public static List<bool> GenerateModules(string text, int quietZoneModules = 10)
    {
        var symbols = EncodeToSymbols(text);
        var modules = new List<bool>();

        // Leading quiet zone
        for (int i = 0; i < quietZoneModules; i++)
            modules.Add(false);

        foreach (var symbol in symbols)
        {
            if (symbol < 0 || symbol >= Patterns.Length) continue;
            string pattern = Patterns[symbol];
            bool isBar = true;
            foreach (char p in pattern)
            {
                int width = p - '0';
                for (int w = 0; w < width; w++)
                    modules.Add(isBar);
                isBar = !isBar;
            }
        }

        // Trailing quiet zone
        for (int i = 0; i < quietZoneModules; i++)
            modules.Add(false);

        return modules;
    }

    /// <summary>
    /// Generates pure vector SVG markup for the barcode.
    /// Can be embedded directly in HTML via (MarkupString) or used in images.
    /// </summary>
    public static string GenerateSvg(string text, int height = 50, float moduleWidth = 1.5f, int quietZoneModules = 10, bool includeXmlHeader = false)
    {
        var modules = GenerateModules(text, quietZoneModules);
        float totalWidth = modules.Count * moduleWidth;

        var sb = new StringBuilder();
        if (includeXmlHeader)
        {
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        }

        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {totalWidth} {height}\" width=\"100%\" height=\"{height}\" shape-rendering=\"crispEdges\">");
        sb.Append($"<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");

        float currentX = 0;
        int barLength = 0;

        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i])
            {
                barLength++;
            }
            else
            {
                if (barLength > 0)
                {
                    float w = barLength * moduleWidth;
                    sb.Append($"<rect x=\"{currentX}\" y=\"0\" width=\"{w}\" height=\"{height}\" fill=\"#000000\"/>");
                    currentX += w;
                    barLength = 0;
                }
                currentX += moduleWidth;
            }
        }

        if (barLength > 0)
        {
            float w = barLength * moduleWidth;
            sb.Append($"<rect x=\"{currentX}\" y=\"0\" width=\"{w}\" height=\"{height}\" fill=\"#000000\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }
}
