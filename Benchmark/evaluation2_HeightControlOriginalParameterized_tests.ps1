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

# include test cases per Dot-Sourcing
. $PSScriptRoot\func_testCases.ps1

New-Variable -Force -Name global_selected_tests -Option AllScope -Value @()
$global_testValuations = @()

$global_selected_tests = $global_tests | Where { $_.TestCategories.Contains("Variant-Original-Original-Original") -and $_.TestCategories.Contains("Probability") }

#AddParameterizedJsonTestValuations -NamePrefix "HeightControlOriginal_Paramererized_OHD" -SourceFile $PSScriptRoot\HeightControlNormal.json -TargetFile $global_compilate_directory\Analysis\heightcontrol_probabilities.json -VariableToUpdate "OverheadDetectorFalseDetection" -MinValue 0.000001 -MaxValue 0.01 -Steps 25

#AddParameterizedJsonTestValuations -NamePrefix "HeightControlOriginal_Paramererized_HV" -SourceFile $PSScriptRoot\HeightControlNormal.json -TargetFile $global_compilate_directory\Analysis\heightcontrol_probabilities.json -VariableToUpdate "LeftHV" -MinValue 0.000001 -MaxValue 0.1 -Steps 25

AddParameterizedJsonTestValuations -NamePrefix "HeightControlOriginal_Paramererized_LB" -SourceFile $PSScriptRoot\HeightControlNormal.json -TargetFile $global_compilate_directory\Analysis\heightcontrol_probabilities.json -VariableToUpdate "LightBarrierFalseDetection" -MinValue 0.000001 -MaxValue 0.01 -Steps 25