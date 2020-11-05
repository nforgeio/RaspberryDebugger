Raspberry Debugger is a Visual Studio extension intended for debugging .NET Core applications remotely on a Raspberry Pi.

## Highlights:

`WRITE SOMETHING HERE!`

Please submit any issues you encounter to:

https://github.com/nforgeio/RaspberryDebugger/issues

## Changes:

* Updated some package dependencies.

### Installation:

Simply download the `RaspberryDebugger.vsix` file to your workstation and double-click it to have Visual Studio install it.

**IMPORTANT:** Raspberry Debugger is not currently compatible with any .NET 5 SDKs.  If you have a .NET 5 SDK installed you'll need to add this `global.json` file to your project or solution directory so your project will be built using the latest installed .NET 3.1.x SDK:

Save as **global.json** file:
```
{
  "sdk": {
    "version": "3.1.100",
	"rollForward": "latestMinor"
  }
}
```

### Getting Started:

Visit our GitHub project for instructions: https://github.com/nforgeio/RaspberryDebugger

### Build Artifacts and SHA512 signatures:

This is is the Raspberry Debugger VSIX package:

> **RaspberryDebugger.vsix:**
> SHA512: `FILL THIS IN`

### Disclosures:

*Raspberry Debugger is compatible with Raspberry Pi*
*Raspberry Pi is a trademark of the Raspberry Pi Foundation*
