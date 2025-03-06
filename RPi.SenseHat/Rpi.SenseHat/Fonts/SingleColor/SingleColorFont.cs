////////////////////////////////////////////////////////////////////////////
//
//  This file is part of Rpi.SenseHat
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emmellsoft.IoT.Rpi.SenseHat.Fonts.SingleColor;

/// <summary>
/// A one-color font.
/// </summary>
/// <remarks>
/// Constructor.
/// </remarks>
/// <param name="chars">Set of characters.</param>
public class SingleColorFont(IEnumerable<SingleColorCharacter> chars) : Font<SingleColorCharacter>(chars)
{

    /// <summary>
    /// Deserializes an encoded byte-array into a SingleColorFont.
    /// </summary>
    /// <param name="serializedChars">An encoded byte array.</param>
    public static SingleColorFont Deserialize(IEnumerable<byte> serializedChars)
    {
        byte[] bytesArray = [.. serializedChars];

        var chars = new List<SingleColorCharacter>();
        var charColumns = new List<byte>();

        var index = 0;
        var isBeginningOfChar = true;
        var isEscaped = false;
        var currentSymbol = ' ';

        while (index < bytesArray.Length)
        {
            var b = bytesArray[index];
            if (isBeginningOfChar)
            {
                if (index > 0)
                {
                    var c = new SingleColorCharacter(currentSymbol, [.. charColumns]);
                    chars.Add(c);
                    charColumns.Clear();
                }

                // Needs 2 bytes for Unicode
                if (index >= bytesArray.Length - 2)
                {
                    throw new ArgumentException("Beginning of char at the end!");
                }

                currentSymbol = Encoding.Unicode.GetString(bytesArray, index, 2).First();

                index += 1;
                isBeginningOfChar = false;
            }
            else if ((b == 0xFF) && !isEscaped)
            {
                if (index == bytesArray.Length - 1)
                {
                    throw new ArgumentException("Escape byte at the end!");
                }

                if (bytesArray[index + 1] == 0x00)
                {
                    isBeginningOfChar = true;
                    index++;
                }
                else
                {
                    isEscaped = true;
                }
            }
            else
            {
                var mask = 1;
                byte column = 0;

                for (var y = 1; y <= 8; y++)
                {
                    if ((b & mask) == mask)
                    {
                        column = (byte)(column | mask);
                    }

                    mask <<= 1;
                }

                charColumns.Add(column);

                isEscaped = false;
            }

            index++;
        }

        if (index > 0)
        {
            var lastChar = new SingleColorCharacter(currentSymbol, [.. charColumns]);
            chars.Add(lastChar);
        }

        return new SingleColorFont(chars);
    }

    /// <summary>
    /// Serializes the font with its characters into a byte array.
    /// </summary>
    public IEnumerable<byte> Serialize()
    {
        var isFirstChar = true;

        foreach (var character in GetChars())
        {
            if (isFirstChar)
            {
                isFirstChar = false;
            }
            else
            {
                // Next char marker.
                yield return 0xFF;
                yield return 0x00;
            }

            var charBytes = Encoding.Unicode.GetBytes([character.Symbol]);
            if (charBytes.Length != 2)
            {
                throw new ArgumentException("Unexpected number of bytes in unicode character!");
            }

            yield return charBytes[0];
            yield return charBytes[1];

            foreach (var column in character.Columns)
            {
                if (column == 0xFF) // Needs escaping to separate from beginning of "Next char marker".
                {
                    yield return 0xFF;
                }

                yield return column;
            }
        }
    }
}