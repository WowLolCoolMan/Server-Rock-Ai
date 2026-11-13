param(
    [string]$InputDirectory = $PSScriptRoot,
    [string]$OutputDirectory = $PSScriptRoot
)

# Builds six aligned frames per Percy state from the approved transparent state art.
# The small pose variations are deterministic so the atlas can be regenerated without
# introducing visual drift. Runtime consumes the pre-cut PNGs; the atlas is for review.
Add-Type -AssemblyName System.Drawing.Common

$frameSize = 256
$frameCount = 6
$states = @("idle", "thinking", "coding", "coding_malicious", "stuck", "success", "error")
$source = @{}
foreach ($state in $states) { $source[$state] = [System.Drawing.Bitmap]::new((Join-Path $InputDirectory "percy-$state.png")) }
$walkSource = [System.Drawing.Bitmap]::new((Join-Path $InputDirectory "percy-walk-sprite-sheet.png"))
$walkFrame = [int]($walkSource.Width / 4)
$pixelFormat = [System.Drawing.Imaging.PixelFormat]::Format32bppArgb

$poses = @{
    idle = @(@(0, 1, 0, 1.00, 1.00), @(0, 0, 0, 1.00, 1.00), @(0, -1, 0, 1.01, 0.99), @(0, -2, 0, 1.02, 0.98), @(0, -1, 0, 1.01, 0.99), @(0, 0, 0, 1.00, 1.00))
    thinking = @(@(0, 1, -3, 1.00, 1.00), @(0, 0, -2, 1.00, 1.00), @(0, -1, 0, 1.00, 1.00), @(0, -1, 2, 1.00, 1.00), @(0, 0, 1, 1.00, 1.00), @(0, 1, -1, 1.00, 1.00))
    coding = @(@(0, 1, 0, 1.00, 1.00), @(0, -2, 0, 1.01, 0.99), @(0, -4, 0, 1.02, 0.98), @(0, -2, 0, 1.01, 0.99), @(0, 1, 0, 1.00, 1.00), @(0, 2, 0, 1.00, 1.00))
    coding_malicious = @(@(-1, 0, -2, 1.00, 1.00), @(2, 1, 2, 1.01, 1.01), @(-2, 0, -1, 1.02, 1.02), @(2, 1, 2, 1.02, 1.02), @(-1, 0, -2, 1.01, 1.01), @(0, 0, 0, 1.00, 1.00))
    stuck = @(@(0, 1, -4, 1.02, 0.98), @(0, 2, -2, 1.03, 0.97), @(0, 3, 0, 1.04, 0.96), @(0, 3, 2, 1.04, 0.96), @(0, 2, 1, 1.03, 0.97), @(0, 1, -2, 1.02, 0.98))
    success = @(@(0, 0, 0, 1.00, 1.00), @(0, -6, 0, 0.99, 1.02), @(0, -12, 0, 0.98, 1.04), @(0, -6, 0, 0.99, 1.02), @(0, 2, 0, 1.06, 0.94), @(0, 0, 0, 1.00, 1.00))
    error = @(@(-2, 0, -2, 1.00, 1.00), @(4, 0, 3, 1.00, 1.00), @(-4, 0, -3, 1.00, 1.00), @(4, 0, 3, 1.00, 1.00), @(-2, 0, -2, 1.00, 1.00), @(0, 0, 0, 1.00, 1.00))
}

function New-Frame([System.Drawing.Bitmap]$bitmap, [System.Drawing.Rectangle]$sourceRect, $pose) {
    $frame = [System.Drawing.Bitmap]::new($frameSize, $frameSize, $pixelFormat)
    $graphics = [System.Drawing.Graphics]::FromImage($frame)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.TranslateTransform(128 + $pose[0], 128 + $pose[1])
        $graphics.RotateTransform($pose[2])
        $graphics.ScaleTransform($pose[3], $pose[4])
        $graphics.DrawImage($bitmap, [System.Drawing.Rectangle]::new(-128, -128, 256, 256), $sourceRect, [System.Drawing.GraphicsUnit]::Pixel)
    }
    finally { $graphics.Dispose() }
    return $frame
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$atlas = [System.Drawing.Bitmap]::new($frameSize * $frameCount, $frameSize * ($states.Count + 1), $pixelFormat)
$atlasGraphics = [System.Drawing.Graphics]::FromImage($atlas)
$atlasGraphics.Clear([System.Drawing.Color]::Transparent)
try {
    for ($row = 0; $row -lt $states.Count; $row++) {
        $state = $states[$row]
        $strip = [System.Drawing.Bitmap]::new($frameSize * $frameCount, $frameSize, $pixelFormat)
        $stripGraphics = [System.Drawing.Graphics]::FromImage($strip)
        $stripGraphics.Clear([System.Drawing.Color]::Transparent)
        try {
            for ($index = 0; $index -lt $frameCount; $index++) {
                $frame = New-Frame $source[$state] ([System.Drawing.Rectangle]::new(0, 0, 256, 256)) $poses[$state][$index]
                try {
                    $frame.Save((Join-Path $OutputDirectory "percy-$state-$($index + 1).png"), [System.Drawing.Imaging.ImageFormat]::Png)
                    $stripGraphics.DrawImage($frame, $index * $frameSize, 0, $frameSize, $frameSize)
                    $atlasGraphics.DrawImage($frame, $index * $frameSize, $row * $frameSize, $frameSize, $frameSize)
                } finally { $frame.Dispose() }
            }
            $strip.Save((Join-Path $OutputDirectory "percy-$state-frames.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $stripGraphics.Dispose(); $strip.Dispose() }
    }

    $walkStrip = [System.Drawing.Bitmap]::new($frameSize * $frameCount, $frameSize, $pixelFormat)
    $walkGraphics = [System.Drawing.Graphics]::FromImage($walkStrip)
    $walkGraphics.Clear([System.Drawing.Color]::Transparent)
    try {
        $walkOrder = @(0, 1, 2, 3, 2, 1)
        for ($index = 0; $index -lt $frameCount; $index++) {
            $frame = New-Frame $walkSource ([System.Drawing.Rectangle]::new($walkOrder[$index] * $walkFrame, 0, $walkFrame, $walkSource.Height)) @(0, 0, 0, 1.00, 1.00)
            try {
                $frame.Save((Join-Path $OutputDirectory "percy-walk-$($index + 1).png"), [System.Drawing.Imaging.ImageFormat]::Png)
                $walkGraphics.DrawImage($frame, $index * $frameSize, 0, $frameSize, $frameSize)
                $atlasGraphics.DrawImage($frame, $index * $frameSize, $states.Count * $frameSize, $frameSize, $frameSize)
            } finally { $frame.Dispose() }
        }
        $walkStrip.Save((Join-Path $OutputDirectory "percy-walk-frames.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $walkGraphics.Dispose(); $walkStrip.Dispose() }
    $atlas.Save((Join-Path $OutputDirectory "percy-animation-sprite-sheet.png"), [System.Drawing.Imaging.ImageFormat]::Png)
} finally {
    $atlasGraphics.Dispose(); $atlas.Dispose()
    foreach ($bitmap in $source.Values) { $bitmap.Dispose() }
    $walkSource.Dispose()
}
