////////////////////////////////////////////////////////////////////////////
//
//  This file is part of Rpi.SenseHat.Tools
//
//  Copyright (c) 2017, Mattias Larsson
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy of 
//  this software and associated documentation files (the "Software"), to deal in 
//  the Software without restriction, including without limitation the rights to use, 
//  copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the 
//  Software, and to permit persons to whom the Software is furnished to do so, 
//  subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all 
//  copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
//  INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//  PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//  HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//  OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//  SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Emmellsoft.IoT.Rpi.SenseHat.Fonts.MultiColor;
using Emmellsoft.IoT.Rpi.SenseHat.Tools.Font;
using Emmellsoft.IoT.Rpi.SenseHat.Tools.LedBuffer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Emmellsoft.IoT.Rpi.SenseHat.Tools;

public static class Program
{
    public static void Main()
    {
        ColorFontWork();
        LedBufferWork();
        SingleColorFontWork();
        SingleColorTinyFontWork();
    }

    private static void ColorFontWork()
    {
        const string symbols = " ABCDEFGHIJKLMNOPQRSTUVWXYZÅÄÖÉÜabcdefghijklmnopqrstuvwxyzåäöéü0123456789.,?!\"#$%&-+*:;/\\<>()'`=";
        const string relativeImagePath = @"..\..\..\RPi.SenseHat.Demo\Assets\ColorFont.png";

        var dir = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
        var path = Path.GetFullPath(Path.Combine(dir ?? string.Empty, relativeImagePath));
        var fontImageUri = new Uri(path);

        var font = MultiColorFont.LoadFromImage(NativePixelSupport.GetPixels(fontImageUri).Result, symbols, Color.Transparent);
        var chars = font.GetChars().ToArray();
        var widths = chars.Select(c => c.Width).ToArray();
    }

    private static void SingleColorFontWork()
    {
        var bitmap = new Bitmap(@"Font\BWFont.png");
        const string chars = " ABCDEFGHIJKLMNOPQRSTUVWXYZÅÄÖÉÜabcdefghijklmnopqrstuvwxyzåäöéü0123456789.,?!\"#$%&-+*:;/\\<>()'`=";

        var singleColorFont = SingleColorFontBuilder.GetSingleColorFont(bitmap, chars);
        byte[] fontBytes = [.. singleColorFont.Serialize()];
        var fontBytesAsCode = ToCSharp(fontBytes);

        var tuple = SingleColorFontBuilder.GetFontBitmap(fontBytes);
        tuple.Item2.Save(@"Font\BWFont_recreated.png");
    }

    private static void SingleColorTinyFontWork()
    {
        var bitmap = new Bitmap(@"Font\TinyBWFont.png");
        const string chars = " 0123456789ABCDEF+-%*=.:!?/\\'";

        var tinyFont = SingleColorFontBuilder.GetSingleColorFont(bitmap, chars);
        byte[] fontBytes = [.. tinyFont.Serialize()];
        var fontBytesAsCode = ToCSharp(fontBytes);
    }

    private static void LedBufferWork()
    {
        byte[] originalGamma =
        [
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01,
                0x02, 0x02, 0x03, 0x03, 0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0E, 0x0F, 0x11,
                0x12, 0x14, 0x15, 0x17, 0x19, 0x1B, 0x1D, 0x1F
            ];

        var senseHatGamma = FindBestGammaMatch.Best5BitGammaMatch(originalGamma, 2, 3, 0.01);

        byte[] senseHatGammaTable = [.. GammaCalc.Get5BitGamma(senseHatGamma)];
        var senseHatGammaTableAsCode = ToCSharp(senseHatGammaTable);

        byte[] senseHatInverseGammaTable = [.. GammaCalc.Get5To8BitInvertedGamma(senseHatGamma)];
        var senseHatInverseGammaTableAsCode = ToCSharp(senseHatInverseGammaTable);



        var initalPixels = LedBufferSupport.BufferToPixels(LedBufferSupport.GetInitialLedBuffer(), senseHatGamma);
        var initalPixelsAsCode = ToCSharp(initalPixels);

        var initialBufferRecreated = LedBufferSupport.PixelsToBuffer(initalPixels, senseHatGamma);
        var initialBufferRecreatedAsCode = ToCSharp(initialBufferRecreated);
        var originalInitialBufferAsCode = ToCSharp(LedBufferSupport.GetInitialLedBuffer());

        var xxx = initialBufferRecreatedAsCode + "\r\n" + originalInitialBufferAsCode;

        RotationTest.Test();
    }

    private static string ToCSharp(IEnumerable<byte> bytes) => string.Join(", ", bytes.Select(ByteToCSharp));//return string.Join(",", bytes.Select(g => g.ToString().PadLeft(3)));//return String.Join("  ", bytes.Select(g => g.ToString("x2")));

    private static string ToCSharp(Color[,] pixels, bool compact = true)
    {
        var text = new StringBuilder();

        if (compact)
        {
            text.AppendLine("Color[] colors = new[]");
            text.AppendLine("{");

            for (var y = 0; y < 8; y++)
            {
                text.Append("\t");

                for (var x = 0; x < 8; x++)
                {
                    var argb = pixels[x, y].ToArgb();
                    var rgb = argb & 0x00FFFFFF;
                    text.Append("0x" + rgb.ToString("X6"));

                    if ((x < 7) || (y < 7))
                    {
                        text.Append(", ");
                    }
                }

                text.AppendLine();
            }

            text.AppendLine("}");
            text.AppendLine(".Select(rgb => Color.FromArgb(0xFF, (byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF)))");
            text.AppendLine(".ToArray();");
        }
        else
        {
            text.AppendLine("Color[,] colors = new Color[,]");
            text.AppendLine("{");

            for (var y = 0; y < 8; y++)
            {
                text.Append("\t{ ");
                for (var x = 0; x < 8; x++)
                {
                    var color = pixels[x, y];

                    if (x > 0)
                    {
                        text.Append(", ");
                    }

                    text.Append("Color.FromArgb(0xff, ");
                    text.Append(string.Join(", ", new[] { color.R, color.G, color.B }.Select(ByteToCSharp)));
                    text.Append(")");
                }
                text.AppendLine(" },");
            }

            text.AppendLine("};");
        }

        return text.ToString();
    }

    private static string ByteToCSharp(byte value) => "0x" + value.ToString("X2");
}
