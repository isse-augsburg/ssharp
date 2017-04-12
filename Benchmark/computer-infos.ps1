$cpu = Get-WmiObject Win32_Processor
$os = Get-WmiObject Win32_OperatingSystem
$memory = $os.TotalVisibleMemorySize/1024/1024

$output = [System.IO.StreamWriter] "pc.txt"
$output.WriteLine($cpu.Name)
$output.Write($memory)
$output.WriteLine(" GB available RAM")
$output.close()