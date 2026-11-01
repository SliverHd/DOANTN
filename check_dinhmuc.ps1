$conn = New-Object System.Data.SqlClient.SqlConnection("Server=.\SQLEXPRESS;Database=XepLichGiangDay;User Id=sa;Password=123;TrustServerCertificate=True")
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT * FROM dinh_muc_giang_vien"
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
$dt = New-Object System.Data.DataTable
$adapter.Fill($dt) | Out-Null
$conn.Close()
$dt | Format-Table -AutoSize | Out-String | Write-Output
