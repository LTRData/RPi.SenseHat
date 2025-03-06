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
using System.Device.I2c;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0057 // Use range operator

namespace Emmellsoft.IoT.Rpi.SenseHat;

internal sealed class MainI2CDevice(I2cDevice device) : IDisposable
{
    private readonly I2cDevice _device = device;

    public void Dispose() => _device.Dispose();

    internal byte ReadByte(byte address)
    {
        Span<byte> value = stackalloc byte[1];

        _device.WriteRead([address], value);

        return value[0];
    }

    internal void ReadBytes(byte address, Span<byte> values)
        => _device.WriteRead([address], values);

    internal void WriteBytes(byte address, Span<byte> values)
    {
        Span<byte> buffer = stackalloc byte[1 + values.Length];
        buffer[0] = address;
        values.CopyTo(buffer.Slice(1));

        _device.Write(buffer);
    }
}