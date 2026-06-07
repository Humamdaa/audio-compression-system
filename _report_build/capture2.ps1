Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;using System.Runtime.InteropServices;
public class Win {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int n);
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  public struct RECT{public int Left,Top,Right,Bottom;}
}
"@
$AE=[System.Windows.Automation.AutomationElement]; $TS=[System.Windows.Automation.TreeScope]
$root=$AE::RootElement
$shots="C:\Users\GharamOthman\Desktop\audio-compression-system\_report_build\shots"
New-Item -ItemType Directory -Force -Path $shots | Out-Null

function Find-Window($name){ $c=New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty,$name); $root.FindFirst($TS::Children,$c) }
function Wait-Window($name,$sec){ for($i=0;$i -lt ($sec*4);$i++){ $w=Find-Window $name; if($w){return $w}; Start-Sleep -Milliseconds 250 }; $null }
function All-Buttons($parent){ $c=New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty,[System.Windows.Automation.ControlType]::Button); $parent.FindAll($TS::Descendants,$c) }
function Click-Button($el){ $p=$el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern); $p.Invoke() }
function Capture($hwnd,$path){
  [Win]::ShowWindow($hwnd,5)|Out-Null;[Win]::BringWindowToTop($hwnd)|Out-Null;[Win]::SetForegroundWindow($hwnd)|Out-Null
  Start-Sleep -Milliseconds 700
  $r=New-Object Win+RECT;[Win]::GetWindowRect($hwnd,[ref]$r)|Out-Null
  $w=$r.Right-$r.Left;$h=$r.Bottom-$r.Top
  if($w -le 0 -or $h -le 0){Write-Output "  bad rect";return}
  $bmp=New-Object System.Drawing.Bitmap $w,$h;$g=[System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($r.Left,$r.Top,0,0,(New-Object System.Drawing.Size($w,$h)))
  $bmp.Save($path,[System.Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$bmp.Dispose()
  Write-Output "  saved $path ($w x $h)"
}

$exe="C:\Users\GharamOthman\Desktop\audio-compression-system\WindowsFormsApp1\bin\Debug\WindowsFormsApp1.exe"
$wav="C:\Users\GharamOthman\Desktop\audio-compression-system\_report_build\sample.wav"

$env:__COMPAT_LAYER="DPIUNAWARE"
$p=Start-Process $exe -PassThru
Start-Sleep -Seconds 3
$hwnd=$p.MainWindowHandle
[Win]::ShowWindow($hwnd,3)|Out-Null   # SW_MAXIMIZE
Start-Sleep -Milliseconds 800
Capture $hwnd "$shots\A_full_ui.png"

$win=Wait-Window "Audio Compressor Pro" 5
Write-Output ("win found: " + [bool]$win)
$btns=@()
if($win){ $btns=All-Buttons $win; Write-Output ("button count: " + $btns.Count)
  foreach($b in $btns){ Write-Output ("  [" + $b.Current.Name + "]") } }

function PickBtn($pred){ foreach($b in $btns){ if(& $pred $b.Current.Name){ return $b } }; $null }
$load=PickBtn { param($n) $n -match 'Load' }
if($load){ Click-Button $load; Start-Sleep -Milliseconds 1300
  [System.Windows.Forms.SendKeys]::SendWait($wav + "{ENTER}"); Start-Sleep -Milliseconds 1800
  Capture $hwnd "$shots\B_loaded.png" }
$comp=PickBtn { param($n) ($n -match 'Compress') -and ($n -notmatch 'Decompress') }
if($comp){ Click-Button $comp
  $rep=Wait-Window "Compression Report" 8; Start-Sleep -Milliseconds 700
  if($rep){ $rh=[IntPtr]$rep.Current.NativeWindowHandle; Capture $rh "$shots\D_report.png"
    [Win]::SetForegroundWindow($rh)|Out-Null;Start-Sleep -Milliseconds 300;[System.Windows.Forms.SendKeys]::SendWait("{ENTER}");Start-Sleep -Milliseconds 800 }
  else { Write-Output "no report window" }
  Capture $hwnd "$shots\C_charts.png" }

Start-Sleep -Milliseconds 400
if(-not $p.HasExited){ Stop-Process -Id $p.Id -Force }
Write-Output "DONE"