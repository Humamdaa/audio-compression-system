Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,System.Windows.Forms,System.Drawing
Add-Type @"
using System;using System.Runtime.InteropServices;
public class Win {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h,int x,int y,int w,int ht,bool repaint);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int n);
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  public struct RECT{public int Left,Top,Right,Bottom;}
}
"@

$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$root = $AE::RootElement
$shots = "C:\Users\GharamOthman\Desktop\audio-compression-system\_report_build\shots"
New-Item -ItemType Directory -Force -Path $shots | Out-Null

function Find-Window($name){
  $c = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty,$name)
  $root.FindFirst($TS::Children,$c)
}
function Wait-Window($name,$sec){
  for($i=0;$i -lt ($sec*4);$i++){ $w=Find-Window $name; if($w){return $w}; Start-Sleep -Milliseconds 250 }
  $null
}
function Find-Button($parent,[scriptblock]$pred){
  $c = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty,[System.Windows.Automation.ControlType]::Button)
  $all = $parent.FindAll($TS::Descendants,$c)
  foreach($b in $all){ if(& $pred $b.Current.Name){ return $b } }
  $null
}
function Invoke-El($el){
  $p = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
  $p.Invoke()
}
function Capture($hwnd,$path){
  [Win]::ShowWindow($hwnd,5) | Out-Null
  [Win]::BringWindowToTop($hwnd) | Out-Null
  [Win]::SetForegroundWindow($hwnd) | Out-Null
  Start-Sleep -Milliseconds 700
  $r = New-Object Win+RECT
  [Win]::GetWindowRect($hwnd,[ref]$r) | Out-Null
  $w = $r.Right-$r.Left; $h = $r.Bottom-$r.Top
  if($w -le 0 -or $h -le 0){ Write-Output "  bad rect for $path"; return }
  $bmp = New-Object System.Drawing.Bitmap $w,$h
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($r.Left,$r.Top,0,0,(New-Object System.Drawing.Size($w,$h)))
  $bmp.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  Write-Output "  saved $path ($w x $h)"
}

$exe = "C:\Users\GharamOthman\Desktop\audio-compression-system\WindowsFormsApp1\bin\Debug\WindowsFormsApp1.exe"
$wav = "C:\Users\GharamOthman\Desktop\audio-compression-system\_report_build\sample.wav"

$p = Start-Process $exe -PassThru
Start-Sleep -Seconds 3
$hwnd = $p.MainWindowHandle
Write-Output "main hwnd=$hwnd"
[Win]::MoveWindow($hwnd,0,0,1040,1040,$true) | Out-Null
Start-Sleep -Milliseconds 600
Capture $hwnd "$shots\01_initial.png"

$win = Wait-Window "Audio Compressor Pro" 5
if(-not $win){ Write-Output "MAIN WINDOW NOT FOUND via UIA"; }
else {
  $load = Find-Button $win { param($n) $n -match 'Load' }
  Write-Output ("load btn: " + $(if($load){$load.Current.Name}else{'NULL'}))
  if($load){ Invoke-El $load; Start-Sleep -Milliseconds 1200
    [System.Windows.Forms.SendKeys]::SendWait($wav + "{ENTER}")
    Start-Sleep -Milliseconds 1800
    [Win]::MoveWindow($hwnd,0,0,1040,1040,$true) | Out-Null
    Capture $hwnd "$shots\02_loaded.png"
  }
  $comp = Find-Button $win { param($n) ($n -match 'Compress') -and ($n -notmatch 'Decompress') }
  Write-Output ("compress btn: " + $(if($comp){$comp.Current.Name}else{'NULL'}))
  if($comp){ Invoke-El $comp
    $rep = Wait-Window "Compression Report" 8
    Start-Sleep -Milliseconds 600
    if($rep){
      $rh = [IntPtr]$rep.Current.NativeWindowHandle
      Capture $rh "$shots\03_report.png"
      [Win]::SetForegroundWindow($rh) | Out-Null; Start-Sleep -Milliseconds 300
      [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
      Start-Sleep -Milliseconds 800
    } else { Write-Output "REPORT WINDOW NOT FOUND" }
    [Win]::MoveWindow($hwnd,0,0,1040,1040,$true) | Out-Null
    Capture $hwnd "$shots\04_charts.png"
  }
}
Start-Sleep -Milliseconds 400
if(-not $p.HasExited){ Stop-Process -Id $p.Id -Force }
Write-Output "DONE"
Get-ChildItem $shots -Filter *.png | Select-Object Name,@{N='KB';E={[math]::Round($_.Length/1KB,1)}} | Format-Table -AutoSize