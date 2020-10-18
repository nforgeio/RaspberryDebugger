# Raspberry Debugger
Visual Studio Extension for debugging .NET Core applications remotely on a Raspberry Pi.

The Microsoft .NET Core platform is a nice way to develop cross-platform applications for Windows, OS/X and Linux.  .NET Core is also compatible with Raspberry Pi and its Linux based operating system: **Raspberry Pi OS**.

You can use Visual Studio Code to develop and debug .NET Core applications either directly on your Raspberry or remotely from another computer but until today, there's been no easy way to use regular Visual Studio to develop and debug applications for Raspberry.

The new **Raspberry Debugger** Visual Studio extension allows you to code your application on a Windows workstation and then build and debug it on a Raspberry by just pressing **F5 - Start Debugging**.

## Requirements

* Visual Studio (Community Edition or better)
* Raspberry Pi running Raspberry Pi OS 32-bit
* Raspberry user allowed to sudo

## Setting up your Raspberry

After getting your Raspberry setup based on the instructions you received with it, you'll need to perform a couple additional steps to make it ready for remote debugging:

1. Enable SSH so the Raspberry Debugger will be able to connect to your Raspberry remotely.  Start the **Terminal** on your Raspberry and enter these commands:
   ```
   sudo systemctl enable ssh
   sudo systemctl start ssh
   ```

2. Ensure that your Raspberry is connected to the network via WiFi or wired ethernet.  You probably already already did this during the initial Raspberry setup.

3. You'll need to need to know the IP address for your Raspberry.  Go back to the **Terminal** and enter this command:
    ```
    ip -h address
    ```
    You'll see something like this:
    ![Screenshot](/Images/ip-address.png)



_The Raspberry Debugger extension is compatible with Raspberry Pi_
_Raspberry Pi is a trademark of the Raspberry Pi Foundation_