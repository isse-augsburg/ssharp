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


# DeadReckoning
AddTest -Testname "DeadReckoning_FalseFormula" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CreateMarkovChainWithFalseFormula" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DeadReckoning_AllHazards" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CreateMarkovChainWithHazards" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DeadReckoning_AllHazardsWithoutStaticPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CreateMarkovChainWithHazardsWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DeadReckoning_Retraversal1" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CreateMarkovChainWithHazardRetraversal1" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DeadReckoning_Retraversal2" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CreateMarkovChainWithHazardsRetraversal2" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DeadReckoning_FaultsInState" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CreateMarkovChainWithHazardFaultsInState" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DeadReckoning_FaultsOnTrans" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CreateFaultAwareMarkovChainAllFaults" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DeadReckoning_SingleCore" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CalculateHazardSingleCore" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DeadReckoning_NoEarlyTermination" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CalculateHazardWithoutEarlyTermination" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DeadReckoning_LtmdpWithoutFaultsWithPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CalculateLtmdpWithoutFaultsWithPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DeadReckoning_LtmdpWithoutStaticPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CalculateLtmdpWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DeadReckoning_MdpNewStates" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CalculateMdpNewStates" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DeadReckoning_NewStatesConstant" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CalculateMdpNewStatesConstant" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DeadReckoning_MdpFlattened" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DeadReckoning.EvaluationTests.CalculateMdpFlattened" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")



# DegradedMode
AddTest -Testname "DegradedMode_FalseFormula" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CreateMarkovChainWithFalseFormula" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DegradedMode")
AddTest -Testname "DegradedMode_AllHazards" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CreateMarkovChainWithHazards" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DegradedMode")
AddTest -Testname "DegradedMode_AllHazardsWithoutStaticPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CreateMarkovChainWithHazardsWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DegradedMode")
AddTest -Testname "DegradedMode_Retraversal1" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CreateMarkovChainWithHazardRetraversal1" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DegradedMode")
AddTest -Testname "DegradedMode_Retraversal2" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CreateMarkovChainWithHazardsRetraversal2" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DegradedMode")
AddTest -Testname "DegradedMode_FaultsInState" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CreateMarkovChainWithHazardFaultsInState" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DegradedMode")
AddTest -Testname "DegradedMode_FaultsOnTrans" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CreateFaultAwareMarkovChainAllFaults" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DegradedMode")
AddTest -Testname "DegradedMode_SingleCore" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CalculateHazardSingleCore" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DegradedMode")
AddTest -Testname "DegradedMode_NoEarlyTermination" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CalculateHazardWithoutEarlyTermination" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DegradedMode")
AddTest -Testname "DegradedMode_LtmdpWithoutFaultsWithPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CalculateLtmdpWithoutFaultsWithPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DegradedMode_LtmdpWithoutStaticPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CalculateLtmdpWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DegradedMode_MdpNewStates" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CalculateMdpNewStates" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DegradedMode_NewStatesConstant" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CalculateMdpNewStatesConstant" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "DegradedMode_MdpFlattened" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.SmallModels.DegradedMode.EvaluationTests.CalculateMdpFlattened" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")



# RailroadCrossing
AddTest -Testname "RailroadCrossing_FalseFormula" -TestAssembly "SafetySharp.CaseStudies.RailroadCrossing.dll" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.Analysis.EvaluationTests.CreateMarkovChainWithFalseFormula" -TestNunitCategory "" -TestCategories @("VariousEvaluations","RailroadCrossing")
AddTest -Testname "RailroadCrossing_AllHazards" -TestAssembly "SafetySharp.CaseStudies.RailroadCrossing.dll" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.Analysis.EvaluationTests.CreateMarkovChainWithHazards" -TestNunitCategory "" -TestCategories @("VariousEvaluations","RailroadCrossing")
AddTest -Testname "RailroadCrossing_AllHazardsWithoutStaticPruning" -TestAssembly "SafetySharp.CaseStudies.RailroadCrossing.dll" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.Analysis.EvaluationTests.CreateMarkovChainWithHazardsWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","RailroadCrossing")
AddTest -Testname "RailroadCrossing_Retraversal1" -TestAssembly "SafetySharp.CaseStudies.RailroadCrossing.dll" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.Analysis.EvaluationTests.CreateMarkovChainWithHazardRetraversal1" -TestNunitCategory "" -TestCategories @("VariousEvaluations","RailroadCrossing")
AddTest -Testname "RailroadCrossing_Retraversal2" -TestAssembly "SafetySharp.CaseStudies.RailroadCrossing.dll" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.Analysis.EvaluationTests.CreateMarkovChainWithHazardsRetraversal2" -TestNunitCategory "" -TestCategories @("VariousEvaluations","RailroadCrossing")
AddTest -Testname "RailroadCrossing_FaultsInState" -TestAssembly "SafetySharp.CaseStudies.RailroadCrossing.dll" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.Analysis.EvaluationTests.CreateMarkovChainWithHazardFaultsInState" -TestNunitCategory "" -TestCategories @("VariousEvaluations","RailroadCrossing")
AddTest -Testname "RailroadCrossing_FaultsOnTrans" -TestAssembly "SafetySharp.CaseStudies.RailroadCrossing.dll" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.Analysis.EvaluationTests.CreateFaultAwareMarkovChainAllFaults" -TestNunitCategory "" -TestCategories @("VariousEvaluations","RailroadCrossing")
AddTest -Testname "RailroadCrossing_SingleCore" -TestAssembly "SafetySharp.CaseStudies.RailroadCrossing.dll" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.Analysis.EvaluationTests.CalculateHazardSingleCore" -TestNunitCategory "" -TestCategories @("VariousEvaluations","RailroadCrossing")
AddTest -Testname "RailroadCrossing_NoEarlyTermination" -TestAssembly "SafetySharp.CaseStudies.RailroadCrossing.dll" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.Analysis.EvaluationTests.CalculateHazardWithoutEarlyTermination" -TestNunitCategory "" -TestCategories @("VariousEvaluations","RailroadCrossing")
AddTest -Testname "RailroadCrossing_LtmdpWithoutFaultsWithPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.EvaluationTests.CalculateLtmdpWithoutFaultsWithPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "RailroadCrossing_LtmdpWithoutStaticPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.EvaluationTests.CalculateLtmdpWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "RailroadCrossing_MdpNewStates" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.EvaluationTests.CalculateMdpNewStates" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "RailroadCrossing_NewStatesConstant" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.EvaluationTests.CalculateMdpNewStatesConstant" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "RailroadCrossing_MdpFlattened" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.RailroadCrossing.EvaluationTests.CalculateMdpFlattened" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")


# LustrePressureTank
AddTest -Testname "LustrePressureTank_FalseFormula" -TestAssembly "SafetyLustre.CaseStudies.LustreModels.dll" -TestMethod "Lustre_Models.EvaluationTests.CreateMarkovChainWithFalseFormula" -TestNunitCategory "" -TestCategories @("VariousEvaluations","LustrePressureTank")
AddTest -Testname "LustrePressureTank_AllHazards" -TestAssembly "SafetyLustre.CaseStudies.LustreModels.dll" -TestMethod "Lustre_Models.EvaluationTests.CreateMarkovChainWithHazards" -TestNunitCategory "" -TestCategories @("VariousEvaluations","LustrePressureTank")
AddTest -Testname "LustrePressureTank_AllHazardsWithoutStaticPruning" -TestAssembly "SafetyLustre.CaseStudies.LustreModels.dll" -TestMethod "Lustre_Models.EvaluationTests.CreateMarkovChainWithHazardsWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","LustrePressureTank")
AddTest -Testname "LustrePressureTank_Retraversal1" -TestAssembly "SafetyLustre.CaseStudies.LustreModels.dll" -TestMethod "Lustre_Models.EvaluationTests.CreateMarkovChainWithHazardRetraversal1" -TestNunitCategory "" -TestCategories @("VariousEvaluations","LustrePressureTank")
AddTest -Testname "LustrePressureTank_Retraversal2" -TestAssembly "SafetyLustre.CaseStudies.LustreModels.dll" -TestMethod "Lustre_Models.EvaluationTests.CreateMarkovChainWithHazardsRetraversal2" -TestNunitCategory "" -TestCategories @("VariousEvaluations","LustrePressureTank")
AddTest -Testname "LustrePressureTank_FaultsInState" -TestAssembly "SafetyLustre.CaseStudies.LustreModels.dll" -TestMethod "Lustre_Models.EvaluationTests.CreateMarkovChainWithHazardFaultsInState" -TestNunitCategory "" -TestCategories @("VariousEvaluations","LustrePressureTank")
AddTest -Testname "LustrePressureTank_FaultsOnTrans" -TestAssembly "SafetyLustre.CaseStudies.LustreModels.dll" -TestMethod "Lustre_Models.EvaluationTests.CreateFaultAwareMarkovChainAllFaults" -TestNunitCategory "" -TestCategories @("VariousEvaluations","LustrePressureTank")
AddTest -Testname "LustrePressureTank_SingleCore" -TestAssembly "SafetyLustre.CaseStudies.LustreModels.dll" -TestMethod "Lustre_Models.EvaluationTests.CalculateHazardSingleCore" -TestNunitCategory "" -TestCategories @("VariousEvaluations","LustrePressureTank")
AddTest -Testname "LustrePressureTank_NoEarlyTermination" -TestAssembly "SafetyLustre.CaseStudies.LustreModels.dll" -TestMethod "Lustre_Models.EvaluationTests.CalculateHazardWithoutEarlyTermination" -TestNunitCategory "" -TestCategories @("VariousEvaluations","LustrePressureTank")
AddTest -Testname "LustrePressureTank_LtmdpWithoutFaultsWithPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "Lustre_Models.EvaluationTests.CalculateLtmdpWithoutFaultsWithPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "LustrePressureTank_LtmdpWithoutStaticPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "Lustre_Models.EvaluationTests.CalculateLtmdpWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "LustrePressureTank_MdpNewStates" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "Lustre_Models.EvaluationTests.CalculateMdpNewStates" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "LustrePressureTank_NewStatesConstant" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "Lustre_Models.EvaluationTests.CalculateMdpNewStatesConstant" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "LustrePressureTank_MdpFlattened" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "Lustre_Models.EvaluationTests.CalculateMdpFlattened" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")


# HemodialysisMachine
AddTest -Testname "HemodialysisMachine_FalseFormula" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CreateMarkovChainWithFalseFormula" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_AllHazards" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CreateMarkovChainWithBothHazards" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_AllHazardsWithoutStaticPruning" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CreateMarkovChainWithBothHazardsWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_Retraversal1" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CreateMarkovChainWithBothHazardsRetraversal1" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_Retraversal2" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CreateMarkovChainWithBothHazardsRetraversal2" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_FaultsInState" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CreateMarkovChainWithBothHazardsFaultsInState" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_FaultsOnTrans" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CreateFaultAwareMarkovChainAllFaults" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_SingleCore" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CalculateHazardSingleCore" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_UnsuccessfulSingleCore" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CalculateBloodNotCleanedAndDialyzingFinishedSingleCore" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_ContaminationSingleCore" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CalculateIncomingBloodWasNotOkSingleCore" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_UnsuccessfulNoEarlyTermination" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CalculateBloodNotCleanedAndDialyzingFinishedWithoutEarlyTermination" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_ContaminationNoEarlyTermination" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CalculateIncomingBloodWasNotOkWithoutEarlyTermination" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_UnsuccessfulSingleCoreNoEarlyTermination" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CalculateBloodNotCleanedAndDialyzingFinishedSingleCoreWithoutEarlyTermination" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_ContaminationSingleCoreNoEarlyTermination" -TestAssembly "SafetySharp.CaseStudies.HemodialysisMachine.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.Analysis.EvaluationTests.CalculateIncomingBloodWasNotOkSingleCoreWithoutEarlyTermination" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HemodialysisMachine")
AddTest -Testname "HemodialysisMachine_LtmdpWithoutFaultsWithPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.EvaluationTests.CalculateLtmdpWithoutFaultsWithPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "HemodialysisMachine_LtmdpWithoutStaticPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.EvaluationTests.CalculateLtmdpWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "HemodialysisMachine_MdpNewStates" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.EvaluationTests.CalculateMdpNewStates" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "HemodialysisMachine_NewStatesConstant" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.EvaluationTests.CalculateMdpNewStatesConstant" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "HemodialysisMachine_MdpFlattened" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.HemodialysisMachine.EvaluationTests.CalculateMdpFlattened" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")



# HeightControl
AddTest -Testname "HeightControl_FalseFormula" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CreateMarkovChainWithFalseFormula" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_AllHazards" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CreateMarkovChainWithBothHazards" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_AllHazardsWithoutStaticPruning" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CreateMarkovChainWithBothHazardsWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_Retraversal1" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CreateMarkovChainWithBothHazardsRetraversal1" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_Retraversal2" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CreateMarkovChainWithBothHazardsRetraversal2" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_FaultsInState" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CreateMarkovChainWithBothHazardsFaultsInState" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_FaultsOnTransFEndFalse" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CreateFaultAwareMarkovChainLeftDetectorFalse" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_FaultsOnTransFEndMis" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CreateFaultAwareMarkovChainLeftDetectorMis" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_FaultsOnTransFPreFalse" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CreateFaultAwareMarkovChainPositionDetectorFalse" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_FaultsOnTransFPreMis" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CreateFaultAwareMarkovChainPositionDetectorMis" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_FaultsOnTransTwoFaults" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CreateFaultAwareMarkovChainTwoFaults" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_SingleCore" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CalculateHazardSingleCore" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_ColissionSingleCore" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CalculateCollisionSingleCore" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_FalseAlarmSingleCore" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CalculateFalseAlarmSingleCore" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_ColissionNoEarlyTermination" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CalculateCollisionWithoutEarlyTermination" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_FalseAlarmNoEarlyTermination" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CalculateFalseAlarmWithoutEarlyTermination" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_ColissionSingleCoreNoEarlyTermination" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CalculateCollisionSingleCoreWithoutEarlyTermination" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_FalseAlarmSingleCoreNoEarlyTermination" -TestAssembly "SafetySharp.CaseStudies.HeightControl.dll" -TestMethod "SafetySharp.CaseStudies.HeightControl.Analysis.EvaluationTests.CalculateFalseAlarmSingleCoreWithoutEarlyTermination" -TestNunitCategory "" -TestCategories @("VariousEvaluations","HeightControl")
AddTest -Testname "HeightControl_LtmdpWithoutFaultsWithPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.HeightControl.EvaluationTests.CalculateLtmdpWithoutFaultsWithPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "HeightControl_LtmdpWithoutStaticPruning" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.HeightControl.EvaluationTests.CalculateLtmdpWithoutStaticPruning" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "HeightControl_MdpNewStates" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.HeightControl.EvaluationTests.CalculateMdpNewStates" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "HeightControl_NewStatesConstant" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.HeightControl.EvaluationTests.CalculateMdpNewStatesConstant" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")
AddTest -Testname "HeightControl_MdpFlattened" -TestAssembly "SafetySharp.CaseStudies.SmallModels.exe" -TestMethod "SafetySharp.CaseStudies.HeightControl.EvaluationTests.CalculateMdpFlattened" -TestNunitCategory "" -TestCategories @("VariousEvaluations","DeadReckoning")








$global_selected_tests = $global_tests | Where { $_.TestCategories.Contains("VariousEvaluations") }

AddTestValuation -Name "VariousEvaluations"  -Script "copy -Force $PSScriptRoot\HeightControlNormal.json $global_compilate_directory\Analysis\heightcontrol_probabilities.json"  -ResultDir "$PSScriptRoot\VariousEvaluations" -FilesOfTestValuation @("$global_compilate_directory\Analysis\heightcontrol_probabilities.json")

Foreach ($testvaluation in $global_testValuations) {
    ExecuteTestValuation -TestValuation $testvaluation -Tests $global_selected_tests
}
