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



AddTest -Testname "DeadReckoning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.BayesianAnalysis.CalculateHazardProbability" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DegradedMode" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.ExampleAnalysis.CalculateHazardProbability" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DegradedMode")
AddTest -Testname "RailroadCrossing" -TestAssembly "SafetySharp.CaseStudies.RailroadCrossing.dll" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.HazardProbabilityTests.Calculate" -TestNunitCategory "" -TestCategories @("VariousEvaluations","RailroadCrossing")
AddTest -Testname "LustrePressureTank" -TestAssembly "SafetyLustre.CaseStudies.LustreModels.dll" -TestMethod "Lustre_Models.ModelCheckingTests.TankRupture" -TestNunitCategory "" -TestCategories @("VariousEvaluations","LustrePressureTank")
AddTest -Testname "HemodialysisMachine_Unsuccessful" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.HazardProbabilityTests.DialysisFinishedAndBloodNotCleaned" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_Contamination" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.HazardProbabilityTests.IncomingBloodIsContaminated" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HeightControl_Collision" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.HazardProbabilityTests.CalculateCollisionInOriginalDesign" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_FalseAlarm" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.HazardProbabilityTests.CalculateFalseAlarmInOriginalDesign" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")






$global_selected_tests = $global_tests | Where { $_.TestCategories.Contains("VariousEvaluations") }

AddTestValuation -Name "Probabilities"  -Script "copy -Force $PSScriptRoot\HeightControlNormal.json $global_compilate_directory\Analysis\heightcontrol_probabilities.json"  -ResultDir "$PSScriptRoot\Probabilities" -FilesOfTestValuation @("$global_compilate_directory\Analysis\heightcontrol_probabilities.json")

Foreach ($testvaluation in $global_testValuations) {
    ExecuteTestValuation -TestValuation $testvaluation -Tests $global_selected_tests
}
