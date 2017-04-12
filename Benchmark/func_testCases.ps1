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

# It is easy to get the method names in an assembly by extracting them from TestResult.xml which is generated by nunit.exe


New-Variable -Force -Name global_tests -Option AllScope -Value @()
New-Variable -Force -Name global_testValuations -Option AllScope -Value @()

function AddTest($testName, $testAssembly, $testMethod, $testNunitCategory ="", $testCategories = @() )
{
    $newTest = New-Object System.Object
    $newTest | Add-Member -type NoteProperty -name TestName -Value $testName
    $newTest | Add-Member -type NoteProperty -name TestAssembly -Value $testAssembly
    $newTest | Add-Member -type NoteProperty -name TestMethod -Value $testMethod
    $newTest | Add-Member -type NoteProperty -name TestNunitCategory -Value $testNunitCategory
    $newTest | Add-Member -type NoteProperty -name TestCategories -Value $testCategories
    $global_tests += $newTest
}


function AddTestValuation($name,$script="",$resultDir="$PSScriptRoot\Results",$filesOfTestValuation=@())
{
    $testValuation = New-Object System.Object
    $testValuation | Add-Member -type NoteProperty -name Name -Value $name
    $testValuation | Add-Member -type NoteProperty -name Script -Value $script
    $testValuation | Add-Member -type NoteProperty -name ResultDir -Value $resultDir
    $testValuation | Add-Member -type NoteProperty -name FilesOfTestValuation -Value $filesOfTestValuation
    $global_testValuations += $testValuation
}

function UpdateValueInJsonFile($sourceFile,$targetFile,$variableToUpdate,$newValue)
{
    $sourceFileContent = Get-Content $sourceFile
    $sourceJson = ConvertFrom-Json "$sourceFileContent"
    $newValueString = $newValue.ToString([System.Globalization.CultureInfo]::InvariantCulture)
    $sourceJson.$variableToUpdate=$newValueString
    $targetFileContent = ConvertTo-Json $sourceJson
    $targetFileContent | Set-Content $targetFile -Force
}

function AddParameterizedJsonTestValuations($namePrefix,$sourceFile,$targetFile,$variableToUpdate,$minValue,$maxValue,$steps)
{
    if ($steps -le 1) {
        echo "steps must be greater than 1"
    }
    $stepsize = ($maxValue - $minValue) / ($steps-1)
    $currentValue = $minValue
    for ($i=0; $i -lt $steps; $i++)
    {
        $newname = $namePrefix + "_" + $i
        $scriptString = "UpdateValueInJsonFile -SourceFile "+ $sourceFile + " -TargetFile " + $targetFile + " -VariableToUpdate " + $variableToUpdate + " -NewValue " + $currentValue 
        AddTestValuation -Name $newname -Script $scriptString -ResultDir "$PSScriptRoot\$newname" -FilesOfTestValuation @($targetFile)
        $currentValue = $currentValue + $stepsize
    }
}


###############################################
# Pressure Tank
###############################################

#AddTest -Testname "PressureTank_Probability_HazardIsDepleted" -TestAssembly "SafetySharp.CaseStudies.PressureTank.dll" -TestMethod "SafetySharp.CaseStudies.PressureTank.Analysis.HazardProbabilityTests.CalculateHazardIsDepleted"


###############################################
# Height Control
###############################################
. $PSScriptRoot\testCases-heightcontrol.ps1