$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$customer = Start-Process dotnet -ArgumentList 'run --project .\CustomerService --urls http://localhost:5001' -WorkingDirectory $root -PassThru
$order = Start-Process dotnet -ArgumentList 'run --project .\OrderService --urls http://localhost:5002' -WorkingDirectory $root -PassThru

try {
    Start-Sleep -Seconds 5

    Write-Host '1) Happy path: contract respected (Customer API v1)'
    $okBody = @{
        customerId = 1
        items = @(
            @{ sku = 'BK-001'; quantity = 2 },
            @{ sku = 'NT-777'; quantity = 1 }
        )
    } | ConvertTo-Json -Depth 4

    $ok = Invoke-RestMethod -Method Post -Uri 'http://localhost:5002/orders?customerApiVersion=v1' -ContentType 'application/json' -Body $okBody
    $ok | ConvertTo-Json -Depth 8

    Write-Host "`n2) Contract mismatch via Customer API v2 (name -> fullName)"
    try {
        Invoke-RestMethod -Method Post -Uri 'http://localhost:5002/orders?customerApiVersion=v2' -ContentType 'application/json' -Body $okBody
    }
    catch {
        $_.ErrorDetails.Message
    }
}
finally {
    if ($customer -and !$customer.HasExited) { Stop-Process -Id $customer.Id -Force }
    if ($order -and !$order.HasExited) { Stop-Process -Id $order.Id -Force }
}
