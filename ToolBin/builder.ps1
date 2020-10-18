#------------------------------------------------------------------------------
# FILE:         builder.ps1
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

# Performs a clean build of the RaspberryDebugger and publishes the binary
# to the [$/build] folder.
#
# USAGE: powershell -file ./builder.ps1

$rdbgRoot  = "$env:RDBG_ROOT"
$rdbgBuild = "$env:RDBG_BUILD"
$rdbgTools = "$rdbgRoot\Tools"

# NOTE: 
#
# MSBUILD.EXE and DEVENV.EXE don't appear to be capable of actually building thr VSIX.
# MSBUILD fails because an EXE appears to be referenced by the project and DEVENV
# builds the [SdkCatalogChecker] but not the VSIX.
#
# So the VSIX will need to be built manually first.

$originalDir = $pwd
cd $rdbgRoot

# Copy the VSIX package to the build folder.

copy $rdbgRoot\RaspberryDebugger\bin\Release\RaspberryDebugger.vsix $rdbgBuild

# Generate the SHA512 hash.

""
"SHA512: RaspberryDebugger.vsix..."
""

& cat "$rdbgBuild\RaspberryDebugger.vsix" | openssl dgst -sha512 -hex > "$rdbgBuild\RaspberryDebugger.vsix.sha512.txt"

if (-not $?)
{
	""
	"*** SHA512 generation failed ***"
	""
	exit 1
}

cd $originalDir
