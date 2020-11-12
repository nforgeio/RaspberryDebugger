### Release Instructions

1. Switch to the release branch, creating one from **main** named like **release-v1.0** if necessary.

2. Merge **main** into the release branch.

3. Run the **SdkCatalogChecker** to validate the catalog.

4. Increment the version number and update:
   
   * `AssemblyInfo.cs:` both the assembly and file versions
   * `source.extension.vsixmanifest`

5. Update the release notes in: `ReleaseNotes.rtf`

6. Open the solution, set the build configuration to **RELEASE** and then manually clean and build the solution.

7. Run this command to complete the release process, copying the build artifcats to the [$/Build] folder:

   `%RDBG_TOOLBIN%\builder.cmd`

8. Attach `$/Build/RaspberryDebugger.vsix** to the the release.

9. Copy/paste the SHA512 from `$/Build/RaspberryDebugger.vsix.sha512.txt** into the release notes.

10. Update the release notes at: `$/RaspberryDebugger/ReleaseNotes.rtf`

11. Commit any changes and push them to GitHub using a comment like: **RELEASE: v1.0**

12. Publish the GitHub release.

13. Switch back to the **main** branch, merge the changes from the release branch and push **main** to GitHub.

14. Publish to the Visual Studio Marketplace:

    a. Goto [Visual Studio MarketPlace](https://marketplace.visualstudio.com/vs)
    b. Sign via via jeff@lilltek.com (for now)
    c. Click **Publish extensions** at the top-right
    d. Click on the **...** next to **Raspberry Debugger** and select **Edit**
    e. Click the **pencil** icon next to **RaspberryDebugger.vsix** and select the new VSIX file at `$/Build/RaspberryDebugger.vsix`
    f. Review and edit the description and overview as required
    g. Click **Save &amp; Upload** at the bottom of the page

------------------------------------------------
$todo(jefflill): Flesh these out:

15. Sign the extension??

------------------------------------------------

### Post Release Steps

1. Create an .ZIP archive by executing:

    `%RDBG_TOOLBIN%\archive.cmd`

2. Create the next release branch from main named like: release-v1.0" and push it to GitHub.

3. Create a new GitHub release with tag like v1.0 and named like v1.0 and select the next release branch.  Copy `RELEASE-TEMPLATE.md` as the initial release description.
