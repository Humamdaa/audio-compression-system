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
  [DllImport("user32.dll",CharSet=CharSet.Auto)] public static extern IntPtr FindWindow(string c,string n);
  public struct RECT{public int Left,Top,Right,Bottom;}
}
"@
$shots="C:\Users\GharamOthman\Desktop\audio-compression-system\_report_build\shots"

function Capture($hwnd,$path){
  [Win]::ShowWindow($hwnd,5)|Out-Null;[Win]::BringWindowToTop($hwnd)|Out-Null;[Win]::SetForegroundWindow($hwnd)|Out-Null
  Start-Sleep -Milliseconds 600
  $r=New-Object Win+RECT;[Win]::GetWindowRect($hwnd,[ref]$r)|Out-Null
  $w=$r.Right-$r.Left;$h=$r.Bottom-$r.Top
  if($w -le 0 -or $h -le 0){Write-Output "  bad rect";return}
  $bmp=New-Object System.Drawing.Bitmap $w,$h;$g=[System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($r.Left,$r.Top,0,0,(New-Object System.Drawing.Size($w,$h)))
  $bmp.Save($path,[System.Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$bmp.Dispose()
  Write-Output "  saved $(Split-Path $path -Leaf) ($w x $h)"
}
function ClickFrac($hwnd,$fx,$fy){
  $r=New-Object Win+RECT;[Win]::GetWindowRect($hwnd,[ref]$r)|Out-Null
  $x=$r.Left+[int](($r.Right-$r.Left)*$fx); $y=$r.Top+[int](($r.Bottom-$r.Top)*$fy)
  [Win]::SetForegroundWindow($hwnd)|Out-Null; Start-Sleep -Milliseconds 200
  [Win]::SetCursorPos($x,$y)|Out-Null; Start-Sleep -Milliseconds 200
  [Win]::mouse_event(0x0002,0,0,0,[UIntPtr]::Zero); Start-Sleep -Milliseconds 60
  [Win]::mouse_event(0x0004,0,0,0,[UIntPtr]::Zero)
  Write-Output "  click frac($fx,$fy) -> ($x,$y)  win=$($r.Right-$r.Left)x$($r.Bottom-$r.Top)"
}
function ScrollDown($hwnd,$times){
  $r=New-Object Win+RECT;[Win]::GetWindowRect($hwnd,[ref]$r)|Out-Null
  [Win]::SetCursorPos([int](($r.Left+$r.Right)/2),[int](($r.Top+$r.Bottom)/2))|Out-Null; Start-Sleep -Milliseconds 150
  for($i=0;$i -lt $times;$i++){ [Win]::mouse_event(0x0800,0,0,-120,[UIntPtr]::Zero); Start-Sleep -Milliseconds 110 }
}

$exe="C:\Users\GharamOthman\Desktop\audio-compression-system\WindowsFormsApp1\bin\Debug\WindowsFormsApp1.exe"
$wav="C:\Users\GharamOthman\Desktop\audio-compression-system\_report_build\sample.wav"

$env:__COMPAT_LAYER="DPIUNAWARE"
$p=Start-Process $exe -PassThru
Start-Sleep -Seconds 3
$hwnd=$p.MainWindowHandle
[Win]::ShowWindow($hwnd,3)|Out-Null; Start-Sleep -Milliseconds 800   # maximize
Capture $hwnd "$shots\A_full_ui.png"

# Fractions measured from the maximized window (scale-invariant for this bitmap-scaled app)
ClickFrac $hwnd 0.081 0.230           # Load File
Start-Sleep -Milliseconds 1300
[System.Windows.Forms.SendKeys]::SendWait($wav + "{ENTER}")
Start-Sleep -Milliseconds 2200
Capture $hwnd "$shots\B_loaded.png"

ClickFrac $hwnd 0.081 0.612           # Compress
Start-Sleep -Milliseconds 3200
$rh=[Win]::FindWindow($null,"Compression Report")
Write-Output ("report hwnd: " + $rh)
if($rh -ne [IntPtr]::Zero){
  Capture $rh "$shots\D_report.png"
  [Win]::SetForegroundWindow($rh)|Out-Null; Start-Sleep -Milliseconds 300
  [System.Windows.Forms.SendKeys]::SendWait("{ENTER}"); Start-Sleep -Milliseconds 1000
}
Capture $hwnd "$shots\C_charts_top.png"
ScrollDown $hwnd 14
Start-Sleep -Milliseconds 500
Capture $hwnd "$shots\C_charts_bottom.png"

Start-Sleep -Milliseconds 400
if(-not $p.HasExited){ Stop-Process -Id $p.Id -Force }
Write-Output "DONE"