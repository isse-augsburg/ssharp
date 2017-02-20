Here you can find batch files for PowerScript to automatically execute all relevant benchmarks and summarize them in a .csv-File.

These scripts are highly platform dependent. It depends on the local system settings such as system language and system paths.
To use them you have to adjust the paths and the system separator (delimiter) in the script files to your local environment.

* `download_nunit2.ps1` downloads NUnit.exe
* `computer-infos.ps1` writes system infos to pc.txt
* `testCases.ps1` contains the list of test cases to benchmark
* `benchmarkTestCases.ps1` benchmarks all test cases and writes the results to the local directory (one file for each test case)
* `summarizeLocalBenchmarks.ps1` summarizes all results of the local directory to one file


Note: You must run the following command first
  ```Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser```

To Undo
  ```Set-ExecutionPolicy -ExecutionPolicy Restricted -Scope CurrentUser```


[System Separator (Delimiter)](https://answers.microsoft.com/en-us/msoffice/forum/msoffice_excel-mso_other/how-do-i-change-the-system-separator-delimiter-to/9f8d5f2c-940f-4418-b952-bfeac867c03e)