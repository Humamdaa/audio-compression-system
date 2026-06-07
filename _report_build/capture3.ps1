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
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,int d,UIntPtr e);
  public struct RECT{public int Left,Top,Right,Bottom;}
}
"@
$AE=[System.Windows.Automation.AutomationElement]; $TS=[System.Windows.Automation.TreeScope]
$shots="C:\Users\GharamOthman\Desktop\audio-compression-system\_report_build\shots"

function All-Buttons($parent){ $c=New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty,[System.Windows.Automation.ControlType]::Button); $parent.FindAll($TS::Descendants,$c) }
function Find-WindowByName($name){ $c=New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty,$name); $AE::RootElement.FindFirst($TS::Children,$c) }
function Wait-WindowByName($name,$sec){ for($i=0;$i -lt ($sec*4);$i++){ $w=Find-WindowByName $name; if($w){return $w}; Start-Sleep -Milliseconds 250 }; $null }
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
  Write-Output "  saved $(Split-Path $path -Leaf) ($w x $h)"
}
function ScrollDown($hwnd,$times){
  $r=New-Object Win+RECT;[Win]::GetWindowRect($hwnd,[ref]$r)|Out-Null
  [Win]::SetCursorPos([int](($r.Left+$r.Right)/2),[int](($r.Top+$r.Bottom)/2))|Out-Null
  for($i=0;$i -lt $times;$i++){ [Win]::mouse_event(0x0800,0,0,-120,[UIntPtr]::Zero); Start-Sleep -Milliseconds 120 }
}

$exe="C:\Users\GharamOthman\Desktop\audio-compression-system\WindowsFormsApp1\bin\Debug\WindowsFormsApp1.exe"
$wav="C:\Users\GharamOthman\Desktop\audio-compression-system\_report_build\sample.wav"

$env:__COMPAT_LAYER="DPIUNAWARE"
$p=Start-Process $exe -PassThru
Start-Sleep -Seconds 3
$hwnd=$p.MainWindowHandle
[Win]::ShowWindow($hwnd,3)|Out-Null; Start-Sleep -Milliseconds 700

$win=$AE::FromHandle($hwnd)
$btns=All-Buttons $win
Write-Output ("buttons via FromHandle: " + $btns.Count)
foreach($b in $btns){ Write-Output ("  [" + $b.Current.Name + "]") }
function PickBtn($pred){ foreach($b in $btns){ if(& $pred $b.Current.Name){ return $b } }; $null }

$load=PickBtn { param($n) $n -match 'Load' }
if($load){ Click-Button $load; Start-Sleep -Milliseconds 1300
  [System.Windows.Forms.SendKeys]::SendWait($wav + "{ENTER}"); Start-Sleep -Milliseconds 2000
  Capture $hwnd "$shots\B_loaded.png" } else { Write-Output "NO LOAD BTN" }

$comp=PickBtn { param($n) ($n -match 'Compress') -and ($n -notmatch 'Decompress') }
if($comp){ Click-Button $comp
  $rep=Wait-WindowByName "Compression Report" 8; Start-Sleep -Milliseconds 800
  if($rep){ $rh=[IntPtr]$rep.Current.NativeWindowHandle; Capture $rh "$shots\D_report.png"
    [Win]::SetForegroundWindow($rh)|Out-Null;Start-Sleep -Milliseconds 300;[System.Windows.Forms.SendKeys]::SendWait("{ENTER}");Start-Sleep -Milliseconds 900 }
  else { Write-Output "NO REPORT WINDOW" }
  Capture $hwnd "$shots\C_charts_top.png"
  ScrollDown $hwnd 12
  Start-Sleep -Milliseconds 400
  Capture $hwnd "$shots\C_charts_bottom.png"
} else { Write-Output "NO COMPRESS BTN" }

Start-Sleep -Milliseconds 400
if(-not $p.HasExited){ Stop-Process -Id $p.Id -Force }
Write-Output "DONE"