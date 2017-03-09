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
. $PSScriptRoot\func_testCases.ps1

New-Variable -Force -Name results -Option AllScope -Value @()

$resultdir= "$PSScriptRoot\HeightControlVeryLow"

function LoadResult($test)
{
    $newResult = New-Object System.Object
    $newResult | Add-Member -type NoteProperty -name TestName -Value $test.TestName
    $outputfilename = "$resultdir\"+$test.TestName+".out"
    $errorfilename = "$resultdir\"+$test.TestName+".err"
    $output = Get-Content -Path $outputfilename

    #extract TotalTime
    $parsedLine = $output | Where-Object {$_ -match 'Tests run[:] 1, Errors[:] (?<hasError>.), Failures[:] 0, Inconclusive[:] 0, Time[:] (?<time>.*)' }
    $totalTime= $matches['time']
    $newResult | Add-Member -type NoteProperty -name TotalTime -Value $totalTime
    $wasSuccessful = $matches['hasError'] -eq "0"
    $newResult | Add-Member -type NoteProperty -name Successful -Value $wasSuccessful

    $probability = "-"
    $states = "-"
    $transitions = "-"
    $levels = "-"
	$faults = "-"
    $mcssize = "-"
    $avgSizeOfMcs = "-"
    $reasonError = "-"
    
    if ($wasSuccessful) {
        #extract probability stuff
        if($test.TestCategory.EndsWith("Probability")){
            $parsedLine = $output | Where-Object {$_ -match 'Probability of hazard[:] (?<match>.*)' }
            $probability= $matches['match']
        
            $parsedLine = $output | Where-Object {$_ -match 'Discovered (?<states>.*?) states, (?<transitions>.*?) transitions, (?<levels>.*?) levels*.*' }  | Select-Object -Last 1
            $states= $matches['states']
            $transitions= $matches['transitions']
            $levels= $matches['levels']
        }

        if($test.TestCategory.EndsWith("DCCA")){
            $parsedLine = $output | Where-Object {$_ -match 'Of the (?<faults>\d*) faults contained in the model[,]' }
            $faults= $matches['faults']			
			
			$parsedLine = $output | Where-Object {$_ -match 'Minimal Critical Sets[:] (?<mcssize>.*)' }
            $mcssize= $matches['mcssize']
        
            $parsedLine = $output | Where-Object {$_ -match 'Average Minimal Critical Set Cardinality[:] (?<avgSizeOfMcs>.*)' }
            $avgSizeOfMcs= $matches['avgSizeOfMcs']
        }
    }
    else
    {
        $reasonError = "unknown"

        # Error: An unhandled exception of type 'System.OutOfMemoryException' was thrown during model checking: Unable to store an additional transition. Try increasing the successor state capacity.
        $parsedLine = $output | Where-Object {$_ -match '.*Unable to store an additional transition.*' }
        if($parsedLine.Length -gt 0){
            $reasonError = "successor_state_capacity"            
        }

        #  Error: An unhandled exception of type 'System.OutOfMemoryException' was thrown during model checking: Unable to store transitions. Try increasing the transition capacity.
        $parsedLine = $output | Where-Object {$_ -match '.*Unable to store transitions.*' }
        if($parsedLine.Length -gt 0){
            $reasonError = "transition_capacity"            
        }

        # try to get last printed states and transitions, which lead to a fault
        $parsedLine = $output | Where-Object {$_ -match 'Discovered (?<states>.*?) states, (?<transitions>.*?) transitions, (?<levels>.*?) levels*.*' }  | Select-Object -Last 1
        $states= $matches['states']
        $transitions= $matches['transitions']
        $levels= $matches['levels']
    }
    
    $newResult | Add-Member -type NoteProperty -name Probability -Value $probability
    $newResult | Add-Member -type NoteProperty -name States -Value $states
    $newResult | Add-Member -type NoteProperty -name Transitions -Value $transitions
    $newResult | Add-Member -type NoteProperty -name Levels -Value $levels
    $newResult | Add-Member -type NoteProperty -name Faults -Value $faults
    $newResult | Add-Member -type NoteProperty -name MinimalCriticalSets -Value $mcssize
    $newResult | Add-Member -type NoteProperty -name AvgSizeOfMcs -Value $avgSizeOfMcs
    $newResult | Add-Member -type NoteProperty -name ReasonForError -Value $reasonError

    $results += $newResult
}

Foreach ($test in $tests) {
    LoadResult($test)
}

$resultsFile="$resultdir\"+"summarizedBenchmarkResults.csv"

$results | Export-Csv -Path $resultsFile -Encoding ascii -NoTypeInformation -UseCulture

