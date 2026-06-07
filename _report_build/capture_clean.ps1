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
$shots="C:\Users\GharamOthman\Desktop\audio-compression-system\_report_build\shots"
function Capture($hwnd,$path){
  [Win]::ShowWindow($hwnd,5)|Out-Null;[Win]::BringWindowToTop($hwnd)|Out-Null;[Win]::SetForegroundWindow($hwnd)|Out-Null
  Start-Sleep -Milliseconds 700
  $r=New-Object Win+RECT;[Win]::GetWindowRect($hwnd,[ref]$r)|Out-Null
  $w=$r.Right-$r.Left;$h=$r.Bottom-$r.Top
  $bmp=New-Object System.Drawing.Bitmap $w,$h;$g=[System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($r.Left,$r.Top,0,0,(New-Object System.Drawing.Size($w,$h)))
  $bmp.Save($path,[System.Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$bmp.Dispose()
  Write-Output "  saved $(Split-Path $path -Leaf) ($w x $h)"
}
Get-Process WindowsFormsApp1 -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600
$exe="C:\Users\GharamOthman\Desktop\audio-compression-system\WindowsFormsApp1\bin\Debug\WindowsFormsApp1.exe"
$env:__COMPAT_LAYER="DPIUNAWARE"
$p=Start-Process $exe -PassThru
Start-Sleep -Seconds 3
$hwnd=$p.MainWindowHandle
[Win]::ShowWindow($hwnd,3)|Out-Null; Start-Sleep -Milliseconds 1000
Capture $hwnd "$shots\ui_top.png"
# scroll to bottom via mouse wheel (cursor over the window, no clicking)
$r=New-Object Win+RECT;[Win]::GetWindowRect($hwnd,[ref]$r)|Out-Null
[Win]::SetCursorPos([int](($r.Left+$r.Right)/2),[int](($r.Top+$r.Bottom)/2))|Out-Null; Start-Sleep -Milliseconds 200
for($i=0;$i -lt 30;$i++){ [Win]::mouse_event(0x0800,0,0,-120,[UIntPtr]::Zero); Start-Sleep -Milliseconds 90 }
Start-Sleep -Milliseconds 500
Capture $hwnd "$shots\ui_bottom.png"
Start-Sleep -Milliseconds 300
if(-not $p.HasExited){ Stop-Process -Id $p.Id -Force }
Write-Output "DONE"