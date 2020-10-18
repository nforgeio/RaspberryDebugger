# Raspberry Debugger
Visual Studio Extension for debugging .NET Core applications remotely on a Raspberry Pi.

The Microsoft .NET Core platform is a nice way to develop cross-platform applications for Windows, OS/X and Linux.  .NET Core is also compatible with Raspberry Pi and its Linux based operating system: **Raspberry Pi OS**.

You can use Visual Studio Code to develop and debug .NET Core applications either directly on your Raspberry or remotely from another computer but until today, there's been no easy way to use regular Visual Studio to develop and debug applications for Raspberry.

The new **Raspberry Debugger** Visual Studio extension allows you to code your application on a Windows workstation and then build and debug it on a Raspberry by just pressing **F5 - Start Debugging**.

### Requirements

* Visual Studio (Community Edition or better)
* Raspberry Pi running Raspberry Pi OS 32-bit
* Raspberry user allowed to sudo

### Setting up your Raspberry

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
    &nbsp;
    ![Screenshot](/Doc/Images/ip-address.png?raw=true)
    &nbsp;
    You're looking for an **inet** address.  In my case here, my Raspberry is connected to WiFi and so the connection information will be located under the **wlan0** network interface.  I've highlighted the interface and the internet address here.

    When your Raspberry is connected to a wired network, you'll see the IP address beneath the **eth0** network interface which I've also highlighted but there is no IP address listed because my Raspberry is not connected to a wired network.

    Make a note of your Raspberry's IP address, you'll need it to configure a connection in Visual Studio.

4. **Advanced:** Your Raspeberry's IP address may change from time-to-time, depending on your network settings.  I've configured my home router to assign reserve an IP address for my Raspberry so it won't change.  You may need to [configure a static IP address](https://www.raspberrypi.org/documentation/configuration/tcpip/) on the Raspberry itself.

That's all the configuration required on the Raspberry.



### Disclosures

* _The Raspberry Debugger extension is compatible with Raspberry Pi_
* _Raspberry Pi is a trademark of the Raspberry Pi Foundation_