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
#    * https://msdn.microsoft.com/en-us/powershell/reference/5.1/microsoft.powershell.utility/export-csv
#    * http://www.tomsitpro.com/articles/powershell-read-text-files,2-893.html
#
# Note: You must run the following command first
#  Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
# To Undo
#  Set-ExecutionPolicy -ExecutionPolicy Restricted -Scope CurrentUser

# include test cases per Dot-Sourcing
. $PSScriptRoot\testCases.ps1

New-Variable -Force -Name resultDirs -Option AllScope -Value @()
New-Variable -Force -Name results -Option AllScope -Value @()

function LoadResult($test)
{
    $newResult = New-Object System.Object
    $newResult | Add-Member -type NoteProperty -name TestName -Value $test.TestName
    $outputfilename = "$PSScriptRoot\"+$test.TestName+".out"
    $errorfilename = "$PSScriptRoot\"+$test.TestName+".err"
    $output = Get-Content -Path $outputfilename

    #extract TotalTime
    $lineWithTotalTime = $output | Where-Object {$_ -like 'Tests run: 1, Errors: 0, Failures: 0, Inconclusive: 0, Time*' }
    #$lineWithTotalTime = $linesWithTotalTime[0]
    $lineWithTotalTime -match ".*Time[:] (?<time>.*)" 
    $totalTime= $matches['time']
    Write-Output($totalTime)    
    $newResult | Add-Member -type NoteProperty -name TotalTime -Value $totalTime

    #extract probability stuff
    if($test.TestCategory.EndsWith("Probability")){
        $relevantLine = $output | Where-Object {$_ -like 'Probability of hazard:*' }
        $relevantLine -match "Probability of hazard[:] (?<match>.*)" 
        $probability= $matches['match']
        $newResult | Add-Member -type NoteProperty -name Probability -Value $probability
        
        $relevantLine = $output | Where-Object {$_ -like 'Discovered * states, * transitions, * levels*' }  | Select-Object -Last 1
        $relevantLine -match "Discovered (?<states>.*?) states, (?<transitions>.*?) transitions, (?<levels>.*?) levels*.*"
        $states= $matches['states']
        $newResult | Add-Member -type NoteProperty -name States -Value $states
        $transitions= $matches['transitions']
        $newResult | Add-Member -type NoteProperty -name Transitions -Value $transitions
        $levels= $matches['levels']
        $newResult | Add-Member -type NoteProperty -name Levels -Value $levels
    }

    if($test.TestCategory.EndsWith("DCCA")){
        $relevantLine = $output | Where-Object {$_ -like 'Minimal Critical Sets:*' }
        $relevantLine -match "Minimal Critical Sets[:] (?<mcssize>.*)" 
        $mcssize= $matches['mcssize']
        $newResult | Add-Member -type NoteProperty -name MinimalCriticalSets -Value $mcssize
        
        $relevantLine = $output | Where-Object {$_ -like 'Average Minimal Critical Set Cardinality:*' }
        $relevantLine -match "Average Minimal Critical Set Cardinality[:] (?<avgSizeOfMcs>.*)" 
        $avgSizeOfMcs= $matches['avgSizeOfMcs']
        $newResult | Add-Member -type NoteProperty -name AvgSizeOfMcs -Value $avgSizeOfMcs
    }

    $results += $newResult
}

Foreach ($test in $tests) {
    LoadResult($test)
}

$resultsFile="$PSScriptRoot\"+"summarizedBenchmarkResults.csv"

$results | Export-Csv -Path $resultsFile -Encoding ascii -NoTypeInformation -UseCulture

