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

# Example nunit-console.exe D:\Repositories\Universität\ssharp\Binaries\Release\SafetySharp.CaseStudies.PressureTank.dll /run=SafetySharp.CaseStudies.PressureTank.Analysis.HazardProbabilityTests.CalculateHazardIsDepleted"

# include test cases per Dot-Sourcing
. $PSScriptRoot\testCases.ps1


$nunit = "$PSScriptRoot\NUnit2\nunit-console.exe"
$compilate_directory = "D:\Repositories\Universität\ssharp\Binaries\Release"
Set-Location -Path $compilate_directory
$env:Path += ";$PSScriptRoot\NUnit2"

New-Variable -Force -Name testValuations -Option AllScope -Value @()

function AddTestValuation($name,$script="",$resultDir="$PSScriptRoot\Results")
{
    $testValuation = New-Object System.Object
    $testValuation | Add-Member -type NoteProperty -name Name -Value $name
    $testValuation | Add-Member -type NoteProperty -name Script -Value $script
    $testValuation | Add-Member -type NoteProperty -name ResultDir -Value $resultDir
    $testValuations += $testValuation
}

AddTestValuation -Name "HeightControlLowerLightBarriers" -Script "copy -Force $PSScriptRoot\HeightControlLowerLightBarriers.json $compilate_directory\Analysis\heightcontrol_probabilities.json" -ResultDir "$PSScriptRoot\HeightControlLowerLightBarriers"
AddTestValuation -Name "HeightControlNormal"  -Script "copy -Force $PSScriptRoot\HeightControlNormal.json $compilate_directory\Analysis\heightcontrol_probabilities.json" -ResultDir "$PSScriptRoot\HeightControlNormal"
AddTestValuation -Name "HeightControlLowerAllSensors"  -Script "copy -Force $PSScriptRoot\HeightControlLowerAllSensors.json $compilate_directory\Analysis\heightcontrol_probabilities.json" -ResultDir "$PSScriptRoot\HeightControlLowerAllSensors"
AddTestValuation -Name "HeightControlLowerDrivers"  -Script "copy -Force $PSScriptRoot\HeightControlLowerDrivers.json $compilate_directory\Analysis\heightcontrol_probabilities.json" -ResultDir "$PSScriptRoot\HeightControlLowerDrivers"

function ExecuteTest($test,$resultDir)
{
    Write-Output("Testing " +  $test.TestMethod + "`n")
    $arguments = @($test.TestAssembly,"/run",$test.TestMethod)
    if($test.TestCategory){
        $arguments=$arguments+@("/include:"+$test.TestCategory)
    }
    
    $outputfilename = $resultdir + "\" +$test.TestName+".out"
    $errorfilename = $resultdir + "\"+$test.TestName+".err"
    echo $outputfilename
    Start-Process -FilePath $nunit -ArgumentList $arguments -WorkingDirectory $compilate_directory -NoNewWindow -RedirectStandardError $errorfilename -RedirectStandardOutput $outputfilename -Wait
}

function ExecuteTestValuation($testvaluation)
{
    $resultDir = $testvaluation.ResultDir
    Invoke-Expression $testvaluation.Script
    New-Item -ItemType directory -Force -Path $resultDir

    Foreach ($test in $tests) {
        ExecuteTest -Test $test -ResultDir $resultDir
    }

    git log -1 > $resultDir\version
}


Foreach ($testvaluation in $testValuations) {
    ExecuteTestValuation($testvaluation)
}