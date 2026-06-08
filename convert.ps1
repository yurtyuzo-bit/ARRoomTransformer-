Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap("Assets\Textures\AppIcon.png")
$bmp.Save("Assets\Textures\AppIconReal.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Move-Item -Force "Assets\Textures\AppIconReal.png" "Assets\Textures\AppIcon.png"
