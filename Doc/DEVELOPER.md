### Developer Setup Notes

1. Install **Visual Studio 2019 Community 16.3+** from [here](https://visualstudio.microsoft.com/thank-you-downloading-visual-studio/?sku=Community&rel=16)

  * Select **all workloads** on the first panel
  * Click **Individual components**, type *Git* in the search box and select **Git for Windows** and **GitHub extension for Visual Studio**
  * Click **Install** (and take a coffee break)
  * Install **.NET Core SDK 3.1.409 (Windows .NET Core Installer x64)** from [here](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-3.1.409-windows-x64-installer )
  * Apply any pending **Visual Studio updates**
  * **Close** Visual Studio and install any updates

2. *Optional:* Install the [Extensibility Essentials 2019](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.ExtensibilityEssentials2019) Visual Studio extension.  This includes very useful tools for obtaining command IDs and other things.

3. *Optional:* Configure the build **environment variables** required only for releasing the extension:

   * Open **File Explorer**
   * Navigate to the directory holding the cloned repository
   * **Right-click** on **buildenv.cmd** and then **Run as adminstrator**
   * Press ENTER to close the CMD window when the script is finished
   * Restart any Visual Studio instances or command windows to pick up the changes.
