$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5013/api'

function Login($email) {
  $body = @{ email = $email; password = 'Password123!' } | ConvertTo-Json
  return (Invoke-RestMethod -Uri "$base/auth/login" -Method Post -Body $body -ContentType 'application/json').token
}

function Register-Supplier($suffix) {
  $email = "e2e.$suffix.$((Get-Random).ToString())@example.com"
  $reg = @{
    fullName = "E2E Supplier $suffix"
    email = $email
    password = 'Password123!'
    companyName = "E2E Co $suffix"
    registrationNumber = "E2E-$suffix-$(Get-Random)"
    contactPerson = 'Test'
    phone = '+15550009999'
  } | ConvertTo-Json
  $r = Invoke-RestMethod -Uri "$base/auth/register" -Method Post -Body $reg -ContentType 'application/json'
  return @{ email = $email; userId = $r.user.userId; supplierId = $r.supplier.supplierId }
}

function Approve-Supplier($supplierId, $headers) {
  Invoke-RestMethod -Uri "$base/suppliers/$supplierId/status" -Method Patch -Headers $headers -Body (@{approvalStatus='Approved'}|ConvertTo-Json) -ContentType 'application/json' | Out-Null
}

Write-Output '=== WORKFLOW G: Authorization ==='
$poTok = Login 'po.m12@example.com'
$sup = Register-Supplier 'auth'
$supTok = Login $sup.email
try { Invoke-RestMethod -Uri "$base/rfqs" -Method Post -Headers @{Authorization="Bearer $supTok"} -Body '{}' -ContentType 'application/json' } catch { Write-Output "Supplier POST rfqs: $($_.Exception.Response.StatusCode)" }
try { Invoke-RestMethod -Uri "$base/audit-logs" -Headers @{Authorization="Bearer $supTok"} } catch { Write-Output "Supplier audit: $($_.Exception.Response.StatusCode)" }

Write-Output '=== WORKFLOW A + B + C + D (unique lowest) ==='
$hPo = @{ Authorization = "Bearer $poTok" }
$stamp = Get-Date -Format 'HHmmss'
$create = @{
  rfqNumber = "E2E-UNIQ-$stamp"
  title = 'E2E unique lowest'
  description = 'Workflow D'
  currencyCode = 'USD'
  items = @(@{ itemName = 'Part A'; description = ''; quantity = 10; unit = 'EA' })
} | ConvertTo-Json -Depth 5
$rfq = Invoke-RestMethod -Uri "$base/rfqs" -Method Post -Headers $hPo -Body $create -ContentType 'application/json'
$rfqId = $rfq.rfqId
$itemId = $rfq.items[0].rfqItemId
$dl = (Get-Date).AddDays(3).ToUniversalTime().ToString('o')
$round = Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds" -Method Post -Headers $hPo -Body (@{deadline=$dl}|ConvertTo-Json) -ContentType 'application/json'
$roundId = $round.roundId
Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/publish" -Method Patch -Headers $hPo -ContentType 'application/json' | Out-Null
Write-Output "Created rfqId=$rfqId roundId=$roundId itemId=$itemId"

$s1 = Register-Supplier 'low'
$s2 = Register-Supplier 'high'
Approve-Supplier $s1.supplierId $hPo
Approve-Supplier $s2.supplierId $hPo

$tok1 = Login $s1.email
$tok2 = Login $s2.email
$h1 = @{ Authorization = "Bearer $tok1" }
$h2 = @{ Authorization = "Bearer $tok2" }
$bidLow = @{ items = @(@{ rfqItemId = $itemId; unitPrice = 5.0 }) } | ConvertTo-Json
$bidHigh = @{ items = @(@{ rfqItemId = $itemId; unitPrice = 8.0 }) } | ConvertTo-Json
$b1 = Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/bids" -Method Post -Headers $h1 -Body $bidLow -ContentType 'application/json'
$b2 = Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/bids" -Method Post -Headers $h2 -Body $bidHigh -ContentType 'application/json'
Write-Output "Bids: low bidId=$($b1.bidId) high bidId=$($b2.bidId)"

Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/close" -Method Patch -Headers $hPo -ContentType 'application/json' | Out-Null
$ev = Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/evaluate" -Method Post -Headers $hPo -ContentType 'application/json'
Write-Output "Evaluate: tie=$($ev.isLowestRankTied) lowest=$($ev.lowestTotalPrice) count=$($ev.lowestRankSupplierCount)"
$results = Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/evaluation" -Headers $hPo
foreach ($b in $results.bids) { Write-Output "  bid $($b.bidId) rank $($b.rankPosition) total $($b.totalPrice)" }

$apprTok = Login 'approver.m12@example.com'
$hAp = @{ Authorization = "Bearer $apprTok" }
$dec = Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/evaluation/decision" -Method Post -Headers $hAp -Body (@{decision='Approved'}|ConvertTo-Json) -ContentType 'application/json'
Write-Output "Decision: $($dec.status) $($dec.decision)"
$winBid = ($results.bids | Where-Object { $_.rankPosition -eq 1 } | Select-Object -First 1).bidId
$award = Invoke-RestMethod -Uri "$base/rfqs/$rfqId/rounds/$roundId/award" -Method Post -Headers $hAp -Body (@{bidId=$winBid; awardComment='E2E award unique lowest'}|ConvertTo-Json) -ContentType 'application/json'
Write-Output "Award id=$($award.awardId) rfq status check via GET rfq"
$rfqAfter = Invoke-RestMethod -Uri "$base/rfqs/$rfqId" -Headers $hPo
Write-Output "RFQ status after award: $($rfqAfter.status)"

Write-Output '=== WORKFLOW E (tied lowest) ==='
$create2 = @{
  rfqNumber = "E2E-TIE-$stamp"
  title = 'E2E tie lowest'
  description = 'Workflow E'
  currencyCode = 'USD'
  items = @(@{ itemName = 'Widget'; description = ''; quantity = 5; unit = 'EA' })
} | ConvertTo-Json -Depth 5
$rfq2 = Invoke-RestMethod -Uri "$base/rfqs" -Method Post -Headers $hPo -Body $create2 -ContentType 'application/json'
$rfq2Id = $rfq2.rfqId
$item2 = $rfq2.items[0].rfqItemId
$round2 = Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds" -Method Post -Headers $hPo -Body (@{deadline=$dl}|ConvertTo-Json) -ContentType 'application/json'
$round2Id = $round2.roundId
Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/publish" -Method Patch -Headers $hPo -ContentType 'application/json' | Out-Null

$t1 = Register-Supplier 'tieA'
$t2 = Register-Supplier 'tieB'
Approve-Supplier $t1.supplierId $hPo
Approve-Supplier $t2.supplierId $hPo
$samePrice = @{ items = @(@{ rfqItemId = $item2; unitPrice = 12.0 }) } | ConvertTo-Json
Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/bids" -Method Post -Headers @{Authorization="Bearer $(Login $t1.email)"} -Body $samePrice -ContentType 'application/json' | Out-Null
Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/bids" -Method Post -Headers @{Authorization="Bearer $(Login $t2.email)"} -Body $samePrice -ContentType 'application/json' | Out-Null
Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/close" -Method Patch -Headers $hPo -ContentType 'application/json' | Out-Null
$ev2 = Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/evaluate" -Method Post -Headers $hPo -ContentType 'application/json'
Write-Output "Tie eval: isTied=$($ev2.isLowestRankTied) lowest=$($ev2.lowestTotalPrice) suppliersAtLowest=$($ev2.lowestRankSupplierCount)"
$res2 = Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/evaluation" -Headers $hPo
foreach ($b in $res2.bids) { Write-Output "  bid $($b.bidId) rank $($b.rankPosition) total $($b.totalPrice)" }

$rej = Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/evaluation/decision" -Method Post -Headers $hAp -Body (@{decision='Rejected'; reason='Tied lowest price - new round required'}|ConvertTo-Json) -ContentType 'application/json'
Write-Output "Reject decision: $($rej.status) reason=$($rej.reason)"
try {
  Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds/$round2Id/award" -Method Post -Headers $hAp -Body (@{bidId=1; awardComment='should fail'}|ConvertTo-Json) -ContentType 'application/json'
  Write-Output 'ERROR: award should not succeed'
} catch {
  Write-Output "Award blocked (expected): $($_.Exception.Response.StatusCode) $($_.ErrorDetails.Message)"
}
$rfq2After = Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id" -Headers $hPo
Write-Output "RFQ2 status after rejection: $($rfq2After.status)"
$dl3 = (Get-Date).AddDays(10).ToUniversalTime().ToString('o')
try {
  $round3 = Invoke-RestMethod -Uri "$base/rfqs/$rfq2Id/rounds" -Method Post -Headers $hPo -Body (@{deadline=$dl3}|ConvertTo-Json) -ContentType 'application/json'
  Write-Output "Next round created: roundNumber=$($round3.roundNumber) status=$($round3.status)"
} catch {
  Write-Output "Next round: $($_.ErrorDetails.Message)"
}

Write-Output '=== WORKFLOW F: Audit sample ==='
$logs = Invoke-RestMethod -Uri "$base/audit-logs?page=1&pageSize=20&action=RfqAwarded" -Headers $hPo
Write-Output "RfqAwarded audit entries: $($logs.totalCount)"
if ($logs.items.Count -gt 0) {
  $detail = Invoke-RestMethod -Uri "$base/audit-logs/$($logs.items[0].auditId)" -Headers $hPo
  Write-Output "Sample audit action=$($detail.action) entity=$($detail.entityType)/$($detail.entityId)"
}

Write-Output '=== SUMMARY IDs ==='
Write-Output "Unique: rfq $rfqId round $roundId award $($award.awardId)"
Write-Output "Tie: rfq $rfq2Id round $round2Id"