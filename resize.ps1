Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap("Assets\Textures\AppIcon.png")
$bmp120 = new-object System.Drawing.Bitmap($bmp, 120, 120)
$bmp120.Save("Assets\Textures\Icon-120.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp120.Dispose()
$bmp.Dispose()
