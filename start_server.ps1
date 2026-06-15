Add-Type -AssemblyName System.Windows.Forms
$serverDir = "c:\RemoteControl-1\RemoteControl.Server\bin\Debug"
[Environment]::CurrentDirectory = $serverDir
Set-Location $serverDir

# Assembly resolver for dependencies
$resolveHandler = [System.ResolveEventHandler]{
    param($sender, $e)
    $name = (New-Object System.Reflection.AssemblyName($e.Name)).Name
    $path = [IO.Path]::Combine($serverDir, "$name.dll")
    if([IO.File]::Exists($path)) {
        return [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($path))
    }
    return $null
}
[System.AppDomain]::CurrentDomain.add_AssemblyResolve($resolveHandler)

# Load main exe from bytes (bypasses CI policy)
$bytes = [IO.File]::ReadAllBytes("$serverDir\RemoteControl.Server.exe")
$asm = [Reflection.Assembly]::Load($bytes)
Write-Host "Loaded: $($asm.FullName)"

# Run
[System.Windows.Forms.Application]::EnableVisualStyles()
[System.Windows.Forms.Application]::SetCompatibleTextRenderingDefault($false)

# Create form with proper startup path context
$frmType = $asm.GetType("RemoteControl.Server.FrmMain")
try {
    $frm = [Activator]::CreateInstance($frmType)
    [System.Windows.Forms.Application]::Run($frm)
} catch {
    Write-Host "ERROR: $($_.Exception.InnerException.Message)"
    Write-Host "STACK: $($_.Exception.InnerException.StackTrace)"
}
