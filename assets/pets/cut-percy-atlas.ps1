param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,
    [string]$OutputDirectory = $PSScriptRoot
)

# Authoring helper for the generated Percy 4x4 atlas. Runtime loads the pre-cut
# frames, so this script is only needed when replacing the source artwork.
Add-Type -AssemblyName System.Drawing.Common

$source = [System.Drawing.Bitmap]::new((Resolve-Path -LiteralPath $InputPath).Path)
$frameSize = 256
$cellWidth = [int]($source.Width / 4)
$cellHeight = [int]($source.Height / 4)
$pixelFormat = [System.Drawing.Imaging.PixelFormat]::Format32bppArgb

function Save-Cell {
    param([string]$Name, [int]$Row, [int]$Column)

    $frame = [System.Drawing.Bitmap]::new($frameSize, $frameSize, $pixelFormat)
    $graphics = [System.Drawing.Graphics]::FromImage($frame)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $sourceRect = [System.Drawing.Rectangle]::new($Column * $cellWidth, $Row * $cellHeight, $cellWidth, $cellHeight)
        $destinationRect = [System.Drawing.Rectangle]::new(0, 0, $frameSize, $frameSize)
        $graphics.DrawImage($source, $destinationRect, $sourceRect, [System.Drawing.GraphicsUnit]::Pixel)
        $frame.Save((Join-Path $OutputDirectory "percy-$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $frame.Dispose()
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
Save-Cell "idle" 0 0
Save-Cell "thinking" 2 0
Save-Cell "coding" 2 1
Save-Cell "coding_malicious" 2 2
Save-Cell "stuck" 3 0
Save-Cell "success" 3 1
Save-Cell "error" 3 2

$walkSheet = [System.Drawing.Bitmap]::new($frameSize * 4, $frameSize, $pixelFormat)
$walkGraphics = [System.Drawing.Graphics]::FromImage($walkSheet)
try {
    $walkGraphics.Clear([System.Drawing.Color]::Transparent)
    for ($column = 0; $column -lt 4; $column++) {
        $sourceRect = [System.Drawing.Rectangle]::new($column * $cellWidth, $cellHeight, $cellWidth, $cellHeight)
        $destinationRect = [System.Drawing.Rectangle]::new($column * $frameSize, 0, $frameSize, $frameSize)
        $walkGraphics.DrawImage($source, $destinationRect, $sourceRect, [System.Drawing.GraphicsUnit]::Pixel)
    }
    $walkSheet.Save((Join-Path $OutputDirectory "percy-walk-sprite-sheet.png"), [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $walkGraphics.Dispose()
    $walkSheet.Dispose()
    $source.Dispose()
}
