Add-Type -AssemblyName System.Drawing

$outDir = "$PSScriptRoot\..\src\IKVM.VisualStudio.Vsix\Images"

function New-IkvmBitmap([int]$sz) {
	$b  = New-Object System.Drawing.Bitmap($sz, $sz)
	$g  = [System.Drawing.Graphics]::FromImage($b)
	$g.SmoothingMode    = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
	$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
	$g.Clear([System.Drawing.Color]::Transparent)

	# Rounded-rectangle background (deep navy blue)
	$rad     = [int]($sz * 0.14)
	$bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 25, 55, 115))
	$bgPath  = New-Object System.Drawing.Drawing2D.GraphicsPath
	$bgPath.AddArc(0,          0,          $rad*2, $rad*2, 180, 90)
	$bgPath.AddArc($sz-$rad*2, 0,          $rad*2, $rad*2, 270, 90)
	$bgPath.AddArc($sz-$rad*2, $sz-$rad*2, $rad*2, $rad*2, 0,   90)
	$bgPath.AddArc(0,          $sz-$rad*2, $rad*2, $rad*2, 90,  90)
	$bgPath.CloseFigure()
	$g.FillPath($bgBrush, $bgPath)

	# Orange accent stripe at the bottom (clipped inside the rounded rect)
	$g.SetClip($bgPath)
	$accentH     = [int]($sz * 0.16)
	$accentBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 240, 130, 0))
	$g.FillRectangle($accentBrush, 0, $sz - $accentH, $sz, $accentH)
	$g.ResetClip()

	# "IK" text centred, shifted slightly upward to sit above the stripe
	$fSize = [int]($sz * 0.50)
	$font  = New-Object System.Drawing.Font("Segoe UI", $fSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
	$tb    = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
	$sf    = New-Object System.Drawing.StringFormat
	$sf.Alignment     = [System.Drawing.StringAlignment]::Center
	$sf.LineAlignment = [System.Drawing.StringAlignment]::Center
	$rect  = New-Object System.Drawing.RectangleF(0.0, ([float](-$sz * 0.08)), [float]$sz, [float]$sz)
	$g.SetClip($bgPath)
	$g.DrawString("IK", $font, $tb, $rect, $sf)
	$g.ResetClip()

	$g.Dispose()
	return $b
}

# --- PNG (256 px, used by VSIX manifest as marketplace icon) ---
$png = New-IkvmBitmap 256
$png.Save("$outDir\ikvm.png", [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Saved ikvm.png"

# --- ICO (multi-size: 16, 32, 48, 256) ---
$sizes = @(16, 32, 48, 256)
$streams = foreach ($s in $sizes) {
	$bmp = New-IkvmBitmap $s
	$ms  = New-Object System.IO.MemoryStream
	$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
	$ms
}

$ico = New-Object System.IO.MemoryStream
$bw  = New-Object System.IO.BinaryWriter($ico)

# ICONDIR header
$bw.Write([uint16]0)              # reserved
$bw.Write([uint16]1)              # type = icon
$bw.Write([uint16]$sizes.Count)

# Directory entries
$dataOffset = 6 + $sizes.Count * 16
for ($i = 0; $i -lt $sizes.Count; $i++) {
	$s = $sizes[$i]
	$wh = [byte]($(if ($s -eq 256) { 0 } else { $s }))
	$bw.Write($wh)   # width  (0 = 256)
	$bw.Write($wh)   # height (0 = 256)
	$bw.Write([byte]0)                                       # color count
	$bw.Write([byte]0)                                       # reserved
	$bw.Write([uint16]1)                                     # planes
	$bw.Write([uint16]32)                                    # bpp
	$bw.Write([uint32]$streams[$i].Length)
	$bw.Write([uint32]$dataOffset)
	$dataOffset += $streams[$i].Length
}
foreach ($ms in $streams) { $bw.Write($ms.ToArray()) }
$bw.Flush()

[System.IO.File]::WriteAllBytes("$outDir\ikvm.ico", $ico.ToArray())
Write-Host "Saved ikvm.ico"
