param(
    [string]$InputDirectory = $PSScriptRoot,
    [string]$OutputDirectory = $PSScriptRoot
)

# Generates aligned six-frame state and walk strips for every built-in pet.
# Source state art remains the identity reference; only small, deterministic pose
# changes are applied so the loop is smooth and reproducible.
Add-Type -AssemblyName System.Drawing.Common

$frameSize = 256
$frameCount = 6
$pets = @("percy", "rocky", "byte", "sprout", "nova", "ember")
$states = @("idle", "thinking", "coding", "coding_malicious", "stuck", "success", "error")
$pixelFormat = [System.Drawing.Imaging.PixelFormat]::Format32bppArgb

$poses = @{
    idle = @(@(0, 1, -1, 1.00, 1.00), @(1, 0, 0, 1.00, 1.00), @(0, -1, 1, 1.01, 0.99), @(-1, -2, 0, 1.02, 0.98), @(0, -1, -1, 1.01, 0.99), @(1, 0, 1, 1.00, 1.00))
    thinking = @(@(0, 1, -4, 1.00, 1.00), @(1, 0, -2, 1.00, 1.00), @(0, -1, 0, 1.00, 1.00), @(-1, -1, 3, 1.00, 1.00), @(0, 0, 2, 1.00, 1.00), @(1, 1, -1, 1.00, 1.00))
    coding = @(@(0, 1, -1, 1.00, 1.00), @(1, -2, 0, 1.01, 0.99), @(0, -4, 1, 1.02, 0.98), @(-1, -2, 0, 1.01, 0.99), @(0, 0, -1, 1.00, 1.00), @(1, 2, 1, 1.00, 1.00))
    coding_malicious = @(@(-1, 0, -3, 1.00, 1.00), @(2, 1, 2, 1.01, 1.01), @(-2, 0, -1, 1.02, 1.02), @(2, 1, 3, 1.02, 1.02), @(-1, 0, -2, 1.01, 1.01), @(0, 0, 1, 1.00, 1.00))
    stuck = @(@(0, 1, -5, 1.02, 0.98), @(1, 2, -2, 1.03, 0.97), @(0, 3, 0, 1.04, 0.96), @(-1, 3, 2, 1.04, 0.96), @(0, 2, 1, 1.03, 0.97), @(1, 1, -3, 1.02, 0.98))
    success = @(@(0, 0, -1, 1.00, 1.00), @(1, -6, 0, 0.99, 1.02), @(0, -12, 1, 0.98, 1.04), @(-1, -6, 0, 0.99, 1.02), @(0, 2, -1, 1.06, 0.94), @(1, 0, 1, 1.00, 1.00))
    error = @(@(-2, 0, -3, 1.00, 1.00), @(4, 0, 2, 1.00, 1.00), @(-4, 0, -2, 1.00, 1.00), @(4, 0, 3, 1.00, 1.00), @(-2, 0, -1, 1.00, 1.00), @(0, 0, 1, 1.00, 1.00))
    walk = @(@(-4, 1, -2, 0.99, 1.01), @(0, -2, 1, 1.00, 1.00), @(4, 1, 2, 0.99, 1.01), @(4, 1, -2, 0.99, 1.01), @(0, -1, 1, 1.00, 1.00), @(-4, 1, 2, 0.99, 1.01))
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
    for ($y = 0; $y -lt $frameSize; $y++) {
        for ($x = 0; $x -lt $frameSize; $x++) {
            $pixel = $frame.GetPixel($x, $y)
            if ($pixel.A -lt 32) { $frame.SetPixel($x, $y, [System.Drawing.Color]::Transparent) }
        }
    }
    # The generated Percy source contains a few opaque edge pixels. A shared edge
    # cleanup keeps every built-in frame safe on arbitrary app backgrounds.
    for ($y = 0; $y -lt $frameSize; $y++) {
        for ($x = 0; $x -lt $frameSize; $x++) {
            if ($x -lt 6 -or $y -lt 6 -or $x -ge $frameSize - 6 -or $y -ge $frameSize - 6) {
                $frame.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
            }
        }
    }
    return $frame
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$atlas = $null
$atlasGraphics = $null
try {
    foreach ($pet in $pets) {
        $atlas = [System.Drawing.Bitmap]::new($frameSize * $frameCount, $frameSize * ($states.Count + 1), $pixelFormat)
        $atlasGraphics = [System.Drawing.Graphics]::FromImage($atlas)
        $atlasGraphics.Clear([System.Drawing.Color]::Transparent)
        $stateSources = @{}
        foreach ($state in $states) { $stateSources[$state] = [System.Drawing.Bitmap]::new((Join-Path $InputDirectory "$pet-$state.png")) }
        try {
            for ($row = 0; $row -lt $states.Count; $row++) {
                $state = $states[$row]
                $strip = [System.Drawing.Bitmap]::new($frameSize * $frameCount, $frameSize, $pixelFormat)
                $stripGraphics = [System.Drawing.Graphics]::FromImage($strip)
                $stripGraphics.Clear([System.Drawing.Color]::Transparent)
                try {
                    for ($index = 0; $index -lt $frameCount; $index++) {
                        $frame = New-Frame $stateSources[$state] ([System.Drawing.Rectangle]::new(0, 0, 256, 256)) $poses[$state][$index]
                        try {
                            $frame.Save((Join-Path $OutputDirectory "$pet-$state-$($index + 1).png"), [System.Drawing.Imaging.ImageFormat]::Png)
                            $stripGraphics.DrawImage($frame, $index * $frameSize, 0, $frameSize, $frameSize)
                            $atlasGraphics.DrawImage($frame, $index * $frameSize, $row * $frameSize, $frameSize, $frameSize)
                        } finally { $frame.Dispose() }
                    }
                    $strip.Save((Join-Path $OutputDirectory "$pet-$state-frames.png"), [System.Drawing.Imaging.ImageFormat]::Png)
                } finally { $stripGraphics.Dispose(); $strip.Dispose() }
            }

            $walkStrip = [System.Drawing.Bitmap]::new($frameSize * $frameCount, $frameSize, $pixelFormat)
            $walkGraphics = [System.Drawing.Graphics]::FromImage($walkStrip)
            $walkGraphics.Clear([System.Drawing.Color]::Transparent)
            try {
                for ($index = 0; $index -lt $frameCount; $index++) {
                    $frame = New-Frame $stateSources.idle ([System.Drawing.Rectangle]::new(0, 0, 256, 256)) $poses.walk[$index]
                    try {
                        $frame.Save((Join-Path $OutputDirectory "$pet-walk-$($index + 1).png"), [System.Drawing.Imaging.ImageFormat]::Png)
                        $walkGraphics.DrawImage($frame, $index * $frameSize, 0, $frameSize, $frameSize)
                        $atlasGraphics.DrawImage($frame, $index * $frameSize, $states.Count * $frameSize, $frameSize, $frameSize)
                    } finally { $frame.Dispose() }
                }
                $walkStrip.Save((Join-Path $OutputDirectory "$pet-walk-frames.png"), [System.Drawing.Imaging.ImageFormat]::Png)
            } finally { $walkGraphics.Dispose(); $walkStrip.Dispose() }
            $atlas.Save((Join-Path $OutputDirectory "$pet-animation-sprite-sheet.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        } finally {
            foreach ($bitmap in $stateSources.Values) { $bitmap.Dispose() }
            $atlasGraphics.Dispose()
            $atlas.Dispose()
            $atlasGraphics = $null
            $atlas = $null
        }
    }
} finally {
    if ($atlasGraphics) { $atlasGraphics.Dispose() }
    if ($atlas) { $atlas.Dispose() }
}
