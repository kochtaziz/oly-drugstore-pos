$ErrorActionPreference = "Stop"
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
if (!(Test-Path $csc)) {
    throw "C# compiler not found: $csc"
}
New-Item -ItemType Directory -Force "bin" | Out-Null
& $csc /target:winexe /out:bin\OlyDrugstorePOS.exe /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll src\*.cs
