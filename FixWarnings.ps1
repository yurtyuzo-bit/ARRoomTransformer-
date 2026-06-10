$files = Get-ChildItem -Path "Assets\Scripts" -Filter "*.cs" -Recurse
foreach ($f in $files) {
    $c = Get-Content $f.FullName -Raw
    $newC = $c -replace 'FindObjectOfType<', 'FindAnyObjectByType<'
    $newC = $newC -replace 'FindFirstObjectByType<', 'FindAnyObjectByType<'
    if ($c -cne $newC) {
        Set-Content -Path $f.FullName -Value $newC -NoNewline
    }
}
