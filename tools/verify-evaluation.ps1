$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5013/api'

function Login($email) {
  $body = @{ email = $email; password = 'Password123!' } | ConvertTo-Json
  return (Invoke-RestMethod -Uri "$base/auth/login" -Method Post -Body $body -ContentType 'application/json').token
}

function Read-ErrorBody($err) {
  try {
    $stream = $err.Exception.Response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    return $reader.ReadToEnd()
  } catch { return $err.Exception.Message }
}

function Register-Approve($suffix, $hPo) {
  $email = "ver.$suffix.$((Get-Random))@example.com"
  $reg = @{
    fullName = "Verify $suffix"
    email = $email
    password = 'Password123!'
    companyName = "Co $suffix"
    registrationNumber = "VR-$suffix-$(Get-Random)"
  } | ConvertTo-Json
  $r = Invoke-RestMethod -Uri "$base/auth/register" -Method Post -Body $reg -ContentType 'application/json'
  Invoke-RestMethod -Uri "$base/suppliers/$($r.supplier.supplierId)/status" -Method Patch -Headers $hPo -Body '{"approvalStatus":"Approved"}' -ContentType 'application/json' | Out-Null
  return @{ email = $email; token = (Login $email) }
}

$hPo = @{ Authorization = "Bearer $(Login 'po.m12@example.com')" }
$hAp = @{ Authorization = "Bearer $(Login 'approver.m12@example.com')" }
$stamp = Get-Date -Format 'HHmmss'

Write-Output '===== Q1/Q4: GET before evaluate (404 message) ====='
$create = @{
  rfqNumber = "VER-UNIQ-$stamp"
  title = 'Verify unique lowest'
  currencyCode = 'USD'
  items = @(@{ itemName = 'Part'; description = ''; quantity = 10; unit = 'EA' })
} | ConvertTo-Json -Depth 5
$rfq = Invoke-RestMethod -Uri "$base/rfqs" -Method Post -Headers $hPo -Body $create -ContentType 'application/json'
$rfqId = $rfq.rfqId
$itemId = $rfq.items[0].rfqItemId
$dl = (Get-Date).AddDays(3).ToUniversalTime().ToString('o')
$round = Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds" -Method Post -Headers $hPo -Body (@{ deadline = $dl } | ConvertTo-Json) -ContentType 'application/json'
$roundId = $round.roundId
Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/publish" -Method Patch -Headers $hPo | Out-Null

$low = Register-Approve 'low' $hPo
$high = Register-Approve 'high' $hPo
$h1 = @{ Authorization = "Bearer $($low.token)" }
$h2 = @{ Authorization = "Bearer $($high.token)" }
Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/bids" -Method Post -Headers $h1 -Body (@{ items = @(@{ rfqItemId = $itemId; unitPrice = 5.0 }) } | ConvertTo-Json) -ContentType 'application/json' | Out-Null
Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/bids" -Method Post -Headers $h2 -Body (@{ items = @(@{ rfqItemId = $itemId; unitPrice = 8.0 }) } | ConvertTo-Json) -ContentType 'application/json' | Out-Null
Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/close" -Method Patch -Headers $hPo | Out-Null

try {
  Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/evaluation" -Headers $hPo | Out-Null
  Write-Output 'FAIL: GET should 404 before evaluate'
} catch {
  $body = Read-ErrorBody $_
  Write-Output "PRE_GET_STATUS=$([int]$_.Exception.Response.StatusCode) BODY=$body"
}

Write-Output '===== Q3: POST evaluate + persist ====='
$post = Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/evaluate" -Method Post -Headers $hPo
Write-Output "POST evaluatedBidCount=$($post.evaluatedBidCount) lowestTotal=$($post.lowestTotalPrice) isTied=$($post.isLowestRankTied) lowestRank=$($post.lowestRank)"

Write-Output '===== Q4: GET results (PO + Approver) ====='
$getPo = Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/evaluation" -Headers $hPo
$getAp = Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/evaluation" -Headers $hAp
foreach ($b in $getPo.bids) { Write-Output "PO bid $($b.bidId) rank=$($b.rankPosition) total=$($b.totalPrice) company=$($b.companyName)" }
$match = ($getPo.bids | ConvertTo-Json -Compress) -eq ($getAp.bids | ConvertTo-Json -Compress)
Write-Output "PO_APPROVER_BIDS_MATCH=$match"

Write-Output '===== Q7: tied lowest ====='
$create2 = @{
  rfqNumber = "VER-TIE-$stamp"
  title = 'Verify tie'
  currencyCode = 'USD'
  items = @(@{ itemName = 'W'; description = ''; quantity = 5; unit = 'EA' })
} | ConvertTo-Json -Depth 5
$rfq2 = Invoke-RestMethod -Uri "$base/rfqs" -Method Post -Headers $hPo -Body $create2 -ContentType 'application/json'
$rfq2Id = $rfq2.rfqId
$item2 = $rfq2.items[0].rfqItemId
$round2 = Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds" -Method Post -Headers $hPo -Body (@{ deadline = $dl } | ConvertTo-Json) -ContentType 'application/json'
$round2Id = $round2.roundId
Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/publish" -Method Patch -Headers $hPo | Out-Null
$t1 = Register-Approve 'tA' $hPo
$t2 = Register-Approve 'tB' $hPo
$same = @{ items = @(@{ rfqItemId = $item2; unitPrice = 12.0 }) } | ConvertTo-Json
Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/bids" -Method Post -Headers @{ Authorization = "Bearer $($t1.token)" } -Body $same -ContentType 'application/json' | Out-Null
Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/bids" -Method Post -Headers @{ Authorization = "Bearer $($t2.token)" } -Body $same -ContentType 'application/json' | Out-Null
Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/close" -Method Patch -Headers $hPo | Out-Null
$ev2 = Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/evaluate" -Method Post -Headers $hPo
$res2 = Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/evaluation" -Headers $hPo
Write-Output "TIE isTied=$($ev2.isLowestRankTied) suppliersAtLowest=$($ev2.lowestRankSupplierCount)"
foreach ($b in $res2.bids) { Write-Output "  tie bid $($b.bidId) rank=$($b.rankPosition) total=$($b.totalPrice)" }

Write-Output '===== repeat POST idempotent ====='
$rep = Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/evaluate" -Method Post -Headers $hPo
Write-Output "REPEAT count=$($rep.evaluatedBidCount)"

Write-Output "BROWSER_RFQ=$rfqId BROWSER_ROUND=$roundId"
Write-Output "BROWSER_RFQ2=$rfq2Id BROWSER_ROUND2=$round2Id"