try {
    $r = Invoke-WebRequest -Uri 'http://localhost:5046/' -UseBasicParsing -TimeoutSec 10
    Write-Host ("Status: " + $r.StatusCode)
    Write-Host ("Length: " + $r.Content.Length)
    if ($r.Content -match '<title>([^<]*)</title>') { Write-Host ("Title: " + $matches[1]) }
} catch {
    Write-Host ("ERR: " + $_.Exception.Message)
}
