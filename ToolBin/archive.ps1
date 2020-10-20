#------------------------------------------------------------------------------
# FILE:         archive.ps1
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2005-2020 by neonFORGE, LLC.  All rights reserved.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# Creates a ZIP archive that includes all of the RaspberryDebuger source 
# files.  This deletes all build binary files as well as all other files 
# that are not part of the source tree before generating the archive.
#
# USAGE: powershell -file ./archive.ps1 [-target PATH]
#
# OPTIONS:
#
#       -target PATH    - optionally specifies where the archive file
#                         should be written.  This defaults to:
#                       
#                           C:\RaspberryDebugger.zip

param 
(
	$target = "C:\RaspberryDebugger.zip"
)

$rdbgRoot = "$env:RDBG_ROOT"

# Removes the [$/Build] and all [bin] and [obj] folders.

"ARCHIVE: Removing binaries"
Remove-Item "$rdbgRoot\RaspberryDebugger\bin\*" -Recurse -ErrorAction Ignore
Remove-Item "$rdbgRoot\RaspberryDebugger\obj\*" -Recurse -ErrorAction Ignore
Remove-Item "$rdbgRoot\SdkCatalogChecker\bin\*" -Recurse -ErrorAction Ignore
Remove-Item "$rdbgRoot\SdkCatalogChecker\obj\*" -Recurse -ErrorAction Ignore

# Remove the contents of [$/packages].

"ARCHIVE: Removing packages"
Remove-Item "$rdbgRoot\packages\*" -Recurse -ErrorAction Ignore

# Zip the archive

"ARCHIVE: Writing [$target]..."
Remove-Item "$target" -ErrorAction Ignore

# I would have used [Compress-Archive] here but that throws an [OutOfMemoryException].
# It appears that this stupid Cmdlet actually loads the entire collection of files
# being archived into RAM and this exceeds default Powershell RAM allocation (GRRR...)
#
# We'll use [7-zip] instead.

7z a -tzip -r -mmt4 -mx9 -bsp1 "$target" "$rdbgRoot"

" "
"**************************"
"*** ARCHIVING COMPLETE ***"
"**************************"
" "
"OUTPUT: $target"
" "
