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
. .\testCases.ps1

New-Variable -Force -Name resultDirs -Option AllScope -Value @()
New-Variable -Force -Name results -Option AllScope -Value @()

function LoadResult($test)
{
    $outputfilename = "$PSScriptRoot\"+$test.TestName+".out"
    $errorfilename = "$PSScriptRoot\"+$test.TestName+".err"
    $output = Get-Content -Path $outputfilename
    $lineWithTotalTime = $output | Where-Object {$_ -like 'Tests run: 1, Errors: 0, Failures: 0, Inconclusive: 0, Time*' }
    #$lineWithTotalTime = $linesWithTotalTime[0]
    $lineWithTotalTime -match ".*Time[:] (?<time>.*)" 
    $totalTime= $matches['time']
    Write-Output($totalTime)
    
    $newResult = New-Object System.Object
    $newResult | Add-Member -type NoteProperty -name TestName -Value $test.TestName
    $newResult | Add-Member -type NoteProperty -name TotalTime -Value $totalTime
    $results += $newResult
}

Foreach ($test in $tests) {
    LoadResult($test)
}

$resultsFile="$PSScriptRoot\"+"summarizedBenchmarkResults.csv"

$results | Export-Csv -Path $resultsFile -Encoding ascii -NoTypeInformation -Delimiter ';' #delimineter ';' for german excel

