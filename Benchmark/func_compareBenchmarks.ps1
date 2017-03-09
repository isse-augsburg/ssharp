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

New-Variable -Force -Name global_resultDirs -Option AllScope -Value @()
New-Variable -Force -Name global_compareResults -Option AllScope -Value @()

function AddResultDir($name,$resultDir)
{
    $newDir = New-Object System.Object
    $newDir | Add-Member -type NoteProperty -name Name -Value $name
    $newDir | Add-Member -type NoteProperty -name Dir -Value $resultdir
    $resultsFile="$resultDir\"+"summarizedBenchmarkResults.csv"
    $oldresults = Import-Csv $resultsFile -Encoding ascii -UseCulture
    $newDir | Add-Member -type NoteProperty -name Results -Value $oldresults
    $global_resultDirs += $newDir
}

function LoadResults($tests,$counter,$interestingValues )
{
    $test = $tests[$counter]
    $newResult = New-Object System.Object
    $newResult | Add-Member -type NoteProperty -name TestName -Value $test.TestName
    
    Foreach ($interestingValue in $interestingValues) {
        #$newResult | Add-Member -type NoteProperty -name $interestingValue -Value $mcssize

        Foreach ($resultDir in $global_resultDirs){
            echo $resultDir
            $entryname=$resultDir.Name+$interestingValue
            $entryvalue= $resultDir.Results[$counter].$interestingValue
            $newResult | Add-Member -type NoteProperty -name $entryname -Value $entryvalue
        }
    }
    $global_compareResults += $newResult
}

function CompareSummarized($tests, $interestingValues) {
    $global_compareResults = @()

    for ($counter=0; $counter -lt $tests.Length; $counter++) {
        LoadResults -Tests $tests -Counter $counter -InterestingValues $interestingValues
    }
    
    $resultsFile="$PSScriptRoot\"+"compareBenchmarks.csv"
    $global_compareResults | Export-Csv -Path $resultsFile -Encoding ascii -NoTypeInformation -UseCulture
}