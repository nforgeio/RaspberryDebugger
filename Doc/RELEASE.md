### Release Instructions

1. Increment the version number and update:
   
   * `AssemblyInfo.cs:` both the assembly and file versions
   * `source.extension.vsixmanifest`

2. Update the release notes in: `ReleaseNotes.rtf` and `source.extension.vsixmanifest`

3. Open the solution, set the build configuration to **RELEASE** and then manually clean and build the solution.

4. Run this command to complete the release process, copying the build artifcats to the [$/Build] folder:

   `%RDBG_TOOLBIN%\builder.cmd`

5. Create a release branch from **main** named for the release like **release-v1.0** and push it to GitHub.

6. Create a new GitHub release named the same as the new version and set the releast branch to the newly created branch.

7. Copy `RELEASE-TEMPLATE.md` into the release notes and describe the changes including any related issues.

8. Attach `$/Build/RaspberryDebugger.vsix** to the the release.

9. Copy/paste the SHA512 from `$/Build/RaspberryDebugger.vsix.sha512.txt** into the release notes.

10. Publish the GitHub release.

11. Commit any changes and push them to GitHub using a comment like: **RELEASE v1.0**

12. Switch back to the **main** branch, merge the changes from the release branch and push **main** to GitHub.

------------------------------------------------
$todo(jefflill): Flesh these out:

13. Build and sign the extension??

14. Release to Visual Studio Marketplace??
------------------------------------------------

### Post Release Steps

1. Create the next release branch from main named like: release-v1.0" and push it to GitHub.

2. Create a new GitHub release with tag like v1.0 and named like v1.0 and select the next release branch.  Copy `RELEASE-TEMPLATE.md` as the initial release description.
