# RPi.SenseHat

C# libraries for the Raspberry Pi Sense HAT's LED matrix, joystick and sensors.

This is the LTRData fork of [Emmellsoft's RPi.SenseHat](https://github.com/emmellsoft/RPi.SenseHat). The main libraries now target .NET 8, 9 and 10 and access hardware through `System.Device.I2c` from `System.Device.Gpio`. The original Windows IoT/UWP demo and development utilities remain in the repository.

## Projects

All project paths below are relative to [RPi.SenseHat](RPi.SenseHat).

| Project | Purpose | Targets |
| --- | --- | --- |
| [Rpi.SenseHat](RPi.SenseHat/Rpi.SenseHat/Rpi.SenseHat.csproj) | Main API: 8×8 RGB display, rotation, mirroring, gamma correction, fonts, joystick and sensor readings. Depends on RTIMULibCS. | `net8.0;net9.0;net10.0` |
| [RTIMULibCS](RPi.SenseHat/RTIMULibCS/RTIMULibCS.csproj) | Sensor drivers, calibration and sensor fusion, under the `RichardsTech.Sensors` namespace. | `net8.0;net9.0;net10.0` |
| [RPi.SenseHat.Tools](RPi.SenseHat/RPi.SenseHat.Tools/RPi.SenseHat.Tools.csproj) | Windows development helpers for bitmap fonts, gamma and rotation calculations. | `net10.0-windows` |
| [RPi.SenseHat.Demo](RPi.SenseHat/RPi.SenseHat.Demo/RPi.SenseHat.Demo.csproj) | Historical Windows IoT/UWP application with display, joystick and sensor examples. | UWP, Windows SDK 10.0.17763.0 |

[Shared package metadata](RPi.SenseHat/Directory.build.props) assigns the library package IDs `LTRData.Rpi.SenseHat` and `LTRData.RTIMULibCS`. The main API retains the `Emmellsoft.IoT.Rpi.SenseHat` namespace. Both libraries reference `System.Device.Gpio` and `Newtonsoft.Json`.

## Hardware and runtime

The [factory](RPi.SenseHat/Rpi.SenseHat/SenseHatFactory.cs) opens I²C bus **1** and uses these devices:

| Component | Driver/device used by this fork |
| --- | --- |
| LED matrix and joystick | Sense HAT controller at address `0x46` |
| Accelerometer, gyroscope and magnetometer | LSM9DS1 |
| Pressure | LPS25H |
| Temperature and humidity | HTS221 |

These devices and the bus number are selected in the source; the factory does not detect alternative sensor chipsets. Check the hardware in your HAT revision before assuming compatibility.

Running an application requires a matching .NET runtime, an accessible I²C bus and an operating system/backend supported by `System.Device.Gpio`. Enable I²C and give the application permission to access the device. The modern libraries do not require a UWP application or the old Visual Studio remote-debugging workflow.

## Build from source

Use the .NET 10 SDK for the current checkout. From the repository root, build the main library and its RTIMULibCS dependency:

```sh
dotnet build RPi.SenseHat/Rpi.SenseHat/Rpi.SenseHat.csproj -c Debug -f net10.0
```

Debug avoids the automatic NuGet packaging enabled by the library projects in Release. Release packaging uses all declared target frameworks; the shared properties also allow the package output directory to be set through `LocalNuGetPath`.

**On a case-sensitive filesystem**, explicitly import the shared properties: the tracked filename is `Directory.build.props`, whereas MSBuild automatically searches for `Directory.Build.props`. For example, from the repository root in Bash:

```sh
dotnet build RPi.SenseHat/Rpi.SenseHat/Rpi.SenseHat.csproj -c Debug -f net10.0 -p:DirectoryBuildPropsPath="$PWD/RPi.SenseHat/Directory.build.props"
```

See [MSBuild's directory customization documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory) for the filename requirement and explicit import property.

Build the library project directly. The solution also contains the historical UWP demo, whose project references cannot consume the current .NET 8–10 library targets. Some solution, demo and tools paths also use different capitalization from the tracked library paths. The full solution is therefore not a portable build entry point.

## Minimal example

Reference `RPi.SenseHat/Rpi.SenseHat/Rpi.SenseHat.csproj` from a .NET console application. This example opens the HAT, lights one pixel and polls temperature and humidity for a few seconds:

```csharp
using System;
using System.Drawing;
using System.Threading.Tasks;
using Emmellsoft.IoT.Rpi.SenseHat;

using var senseHat = await SenseHatFactory.OpenSenseHatAsync();

senseHat.Display.Clear();
senseHat.Display.Screen[0, 0] = Color.Red;
senseHat.Display.Update();

try
{
    for (var i = 0; i < 50; i++)
    {
        senseHat.Sensors.HumiditySensor.Update();

        if (senseHat.Sensors.Temperature is double temperature &&
            senseHat.Sensors.Humidity is double humidity)
        {
            Console.WriteLine($"Temperature: {temperature:F1} °C; humidity: {humidity:F1}%");
        }

        await Task.Delay(100);
    }
}
finally
{
    senseHat.Display.Clear();
    senseHat.Display.Update();
}
```

Display changes are buffered until `Display.Update()`. Sensor readings also require polling: call `ImuSensor.Update()` for motion/orientation, `PressureSensor.Update()` for pressure, and `HumiditySensor.Update()` for temperature/humidity. Reading properties are nullable until valid data arrives and retain the last valid value between updates. Call `Joystick.Update()` before inspecting its direction and enter-key states.

`OpenSenseHatAsync()` creates a new instance that the caller disposes when finished. `GetSenseHat()` instead caches its initialization task and shares the resulting instance; it does not reopen a disposed instance. Serialize access to the factory and hardware objects. The API does not provide a thread-safety guarantee, and the cached factory initialization is not synchronized.

## Historical demos and utilities

The [demo sources](RPi.SenseHat/RPi.SenseHat.Demo/Demos) illustrate scrolling text, sprites, a compass, joystick interaction and sensor polling. [DemoSelector.cs](RPi.SenseHat/RPi.SenseHat.Demo/DemoSelector.cs) selects an example and controls whether HDMI text output is used. Treat these as examples to adapt to a modern application; the retained UWP project needs porting to use the current libraries.

The [tools project](RPi.SenseHat/RPi.SenseHat.Tools) contains development-time calculations and font conversion code with source-selected inputs. It is not needed to use the library.

The [original README at the pre-rewrite revision](https://github.com/LTRData/RPi.SenseHat/blob/4af8d4bd9c06c456b02569cc85ee1e798b10be90/README.md) preserves the old Windows IoT setup instructions for historical reference. The [legacy NuGet directory](NuGetPackage) and [.nuspec](RPi.SenseHat/Rpi.SenseHat/Rpi.SenseHat.nuspec) describe the older upstream package workflow; the current libraries use SDK-style project packaging.

## License and attribution

See [LICENSE.md](LICENSE.md) for the MIT license. The Sense HAT library originates with Mattias Larsson/Emmellsoft; RTIMULibCS includes work credited to richards-tech, LLC. Retain the attribution and license notices in the individual source files.
