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

# include functionality per Dot-Sourcing
. $PSScriptRoot\func_benchmarkTestCases.ps1
# include test cases per Dot-Sourcing
. $PSScriptRoot\func_testCases.ps1

New-Variable -Force -Name global_selected_tests -Option AllScope -Value @()
$global_testValuations = @()

# SafetyLustre.CaseStudies.LustreModels.dll
# SafetySharp.CaseStudies.HeightControl.dll
# SafetySharp.CaseStudies.RailroadCrossing.dll
# SafetySharp.CaseStudies.SmallModels.exe
# SafetySharp.CaseStudies.HemodialysisMachine.exe


AddTest -Testname "Hemodialysis_HazardUnsuccessful" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.HazardProbabilityTests.WaterHeaterPermanentDemandOnCustom" -TestNunitCategory "DialysisFinishedAndBloodNotCleaned" -TestCategories @("HemodialysisMachine","Unhidden")
AddTest -Testname "Hemodialysis_HazardContamination" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.HazardProbabilityTests.WaterHeaterPermanentDemandOnCustom" -TestNunitCategory "IncomingBloodIsContaminated" -TestCategories @("HemodialysisMachine","Unhidden")
AddTest -Testname "HeightControl_Probability_HazardCollision" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.HazardProbabilityTests.Original-Original-Original" -TestNunitCategory "CollisionProbability" -TestCategories @("HeightControl","Variant-Original-Original-Original","Hazard-Collision","Unhidden")
AddTest -Testname "HeightControl_Probability_HazardFalseAlarm" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.HazardProbabilityTests.Original-Original-Original" -TestNunitCategory "FalseAlarmProbability" -TestCategories @("HeightControl","Variant-Original-Original-Original","Hazard-FalseAlarm","Unhidden")
AddTest -Testname "HeightControl_Probability_Prevention-Collision" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.HazardProbabilityTests.Original-Original-Original" -TestNunitCategory "PreventionProbability" -TestCategories @("HeightControl","Variant-Original-Original-Original","Prevention-Collision","Unhidden")
AddTest -Testname "HemodialysisMachine_AllHazardsWithoutStaticPruning" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CreateMarkovChainWithBothHazardsWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("Unhidden","HemodialysisMachine")
AddTest -Testname "HeightControl_AllHazardsWithoutStaticPruning" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CreateMarkovChainWithBothHazardsWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("Unhidden","HeightControl")
AddTest -Testname "HemodialysisMachine_SingleCore" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CalculateHazardSingleCore" -TestNunitCategory "" -TestCategories @("Unhidden","HemodialysisMachine")
AddTest -Testname "HeightControl_SingleCore" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CalculateHazardSingleCore" -TestNunitCategory "" -TestCategories @("Unhidden","HeightControl")
AddTest -Testname "HemodialysisMachine_SingleCore" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CalculateHazardSingleCore" -TestNunitCategory "" -TestCategories @("Unhidden","HemodialysisMachine")
AddTest -Testname "HeightControl_SingleCore" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CalculateHazardSingleCore" -TestNunitCategory "" -TestCategories @("Unhidden","HeightControl")








$global_selected_tests = $global_tests | Where { $_.TestCategories.Contains("Unhidden") }

AddTestValuation -Name "Unhidden"  -Script "copy -Force $PSScriptRoot\HeightControlNormal.json $global_compilate_directory\Analysis\heightcontrol_probabilities.json"  -ResultDir "$PSScriptRoot\Unhidden" -FilesOfTestValuation @("$global_compilate_directory\Analysis\heightcontrol_probabilities.json")


cp "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HeightControlUnhidden.dll" "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HeightControl.dll"
cp "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HemodialysisMachineUnhidden.exe" "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HemodialysisMachine.exe"


Foreach ($testvaluation in $global_testValuations) {
    ExecuteTestValuation -TestValuation $testvaluation -Tests $global_selected_tests
}

cp "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HeightControlHidden.dll" "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HeightControl.dll"
cp "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HemodialysisMachineHidden.exe" "$PSScriptRoot\..\Binaries\Release\SafetySharp.CaseStudies.HemodialysisMachine.exe"