param(
  [switch]
  $WithDebug
)
begin {
  Write-Debug "Enter [$($PSCmdlet.MyInvocation.MyCommand.Name)]..."
  $StartingDebugPreference = $DebugPreference
  if ($WithDebug) {
    Write-Host "Debugging is enabled"
    $DebugPreference = "Continue"
  } else {
    $DebugPreference = "SilentlyContinue"
    Write-Host "Debugging is disabled"
  }
}
process {
  ## Build the project
  dotnet build .\src\Distributary.csproj -c Release
  
  ## Run the tests in PowerShell Core
  pwsh.exe -noprofile -nologo -command { 
    Set-Location D:\Repos\Distributary\ 
    Import-Module ".\src\bin\Release\netstandard2.0\Distributary.dll" 
    Write-Host 'InputMessage as Parameter' 
    Write-OutStream $(Get-Process)[0].Name 
    Write-Host 'InputMessage from Pipeline' 
    $(Get-Process)[0].Name | Write-OutStream 
    
    Write-Host 'InputMessage as param with logfile' 
    
    Write-OutStream $(Get-Process)[0].Name -LogPath 'D:\Repos\Distributary\test.log' -Append -Force
    
    exit 
  }
  
  ## Run the tests in Windows PowerShell 
  powershell.exe -noprofile -nologo -command { 
    Set-Location D:\Repos\Distributary\ 
    Import-Module ".\src\bin\Release\netstandard2.0\Distributary.dll" 
    Write-Host 'InputMessage as Parameter' 
    
    Write-OutStream $(Get-Process)[0].Name
    Write-Host 'InputMessage from Pipeline' 
    
    $(Get-Process)[0].Name | Write-OutStream 
    
    Write-Host 'InputMessage as param with logfile' 
    
    Write-OutStream $(Get-Process)[0].Name -LogPath 'D:\Repos\Distributary\test.log' -Append -Force
    
    exit 
  }
  
}
end {
  $DebugPreference = $StartingDebugPreference
  Write-Debug "Exit [$($PSCmdlet.MyInvocation.MyCommand.Name)]..."
}

