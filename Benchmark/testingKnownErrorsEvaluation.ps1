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

$project_file = "$PSScriptRoot\..\Models\Robot Cell\Robot Cell.csproj"
$test_assembly = "SafetySharp.CaseStudies.RobotCell.dll"
$nunit = "$PSScriptRoot\NUnit2\nunit-console.exe"
$msbuild = "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
$compilate_directory = "$PSScriptRoot\..\Binaries\Release"
$resultdir = "$PSScriptRoot\..\TestEvaluationResults\"
$tmp_dir = "$PSScriptRoot\tmp\"

$env:Path += ";$PSScriptRoot\NUnit2"
New-Item -ItemType directory -Path $resultdir
New-Item -ItemType directory -Path $tmp_dir
Set-Location -Path $compilate_directory

$symbols=@("ENABLE_F1", "ENABLE_F3", "ENABLE_F4", "ENABLE_F5", "ENABLE_F6", "ENABLE_F7")

function CompileProject($symbol)
{
	Write-Output("Compiling...`n")
	$arguments = @("/t:Rebuild","/p:Configuration=Release","/p:DefineConstants=`"TRACE,"+$symbol+"`"","`"$project_file`"")
	$outputfile = $resultdir + "\" + $symbol + ".compile"
	Start-Process -FilePath $msbuild -ArgumentList $arguments -WorkingDirectory $tmp_dir -NoNewWindow -RedirectStandardOutput $outputfile -Wait
}

function ExecuteTest($symbol)
{
    Write-Output("Testing with symbol " + $symbol + "`n")
	Write-Output("===================================`n")

	CompileProject($symbol)

	Write-Output("Testing...`n")
    $arguments = @("/labels","/config:Release","/include:TestEvaluationFast",$test_assembly)
    
    $outputfilename = $resultdir + "\" +$symbol+".out"
    $errorfilename = $resultdir + "\"+$symbol+".err"
    Start-Process -FilePath $nunit -ArgumentList $arguments -WorkingDirectory $compilate_directory -NoNewWindow -RedirectStandardError $errorfilename -RedirectStandardOutput $outputfilename -Wait
}

Foreach ($symbol in $symbols) {
    ExecuteTest($symbol)
}

Set-Location -Path $PSScriptRoot\..\

#git log -1 > $resultdir\version
