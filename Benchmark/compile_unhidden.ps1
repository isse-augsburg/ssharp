# The MIT License (MIT)
# 
# Copyright (c) 2014-2016, Institute for Software & Systems Engineering
# 
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
# 
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
# 
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.
#
# Sources:
#    * https://www.nunit.org/index.php?p=guiCommandLine&r=2.4
#    * https://www.nunit.org/index.php?p=nunit-console&r=2.4
#    * https://msdn.microsoft.com/en-us/powershell/scripting/getting-started/cookbooks/managing-current-location
#    * https://msdn.microsoft.com/en-us/powershell/reference/5.1/microsoft.powershell.management/start-process
#    * https://www.safaribooksonline.com/library/view/windows-powershell-cookbook/9780596528492/ch11s02.html
#    * http://www.get-blog.com/?p=82

# Note: You must run the following command first
#  Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
# To Undo
#  Set-ExecutionPolicy -ExecutionPolicy Restricted -Scope CurrentUser

$project_file_hd = "$PSScriptRoot\..\Models\Hemodialysis Machine\Hemodialysis Machine.csproj"
$project_file_hc = "$PSScriptRoot\..\Models\Height Control\Height Control.csproj"
$msbuild = "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"

function CompileProject($project_file)
{
	Write-Output("Compiling...`n")
	$arguments = @("/t:Rebuild","/p:Configuration=Release","/p:DefineConstants=`"TRACE"+"`"","`"$project_file`"")
	Start-Process -FilePath $msbuild -ArgumentList $arguments -WorkingDirectory ".." -NoNewWindow -Wait
}


cp "$PSScriptRoot\SourceChanges\VehicleUnhidden.cs" "$PSScriptRoot\..\Models\Height Control\Modeling\Vehicles\Vehicle.cs"
CompileProject($project_file_hc)
cp "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HeightControl.dll" "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HeightControlUnhidden.dll"
git checkout "$PSScriptRoot\..\Models\Height Control\Modeling\Vehicles\Vehicle.cs"
CompileProject($project_file_hc)
cp "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HeightControl.dll" "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HeightControlHidden.dll"


cp "$PSScriptRoot\SourceChanges\FlowPortUnhidden.cs" "$PSScriptRoot\..\Models\Hemodialysis Machine\Utilities\BidirectionalFlow\FlowPort.cs"
cp "$PSScriptRoot\SourceChanges\DialyzingFluidFlowUnhidden.cs" "$PSScriptRoot\..\Models\Hemodialysis Machine\Modeling\Flows\DialyzingFluidFlow.cs"
CompileProject($project_file_hd)
cp "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HemodialysisMachine.exe" "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HemodialysisMachineUnhidden.exe"
git checkout "$PSScriptRoot\..\Models\Hemodialysis Machine\Utilities\BidirectionalFlow\FlowPort.cs"
git checkout "$PSScriptRoot\..\Models\Hemodialysis Machine\Modeling\Flows\DialyzingFluidFlow.cs"
CompileProject($project_file_hd)
cp "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HemodialysisMachine.exe" "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HemodialysisMachineHidden.exe"