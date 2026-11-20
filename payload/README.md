# Payload cho bản Windows

Bản Windows nhúng sẵn một gói dữ liệu `du_lieu.zip` (SQLite + nhạc + kịch bản mẫu) vào file thực thi. Gói này không được commit vào repo vì dung lượng ~345 MB và đã có bản nhạc đầy đủ trong `web/audio/`.

## Build source không có payload

`src/GSoundPlayer.App/GSoundPlayer.csproj` chỉ nhúng payload khi file tồn tại:

```xml
<EmbeddedResource Include="..\..\payload\du_lieu.zip" ... Condition="Exists('..\..\payload\du_lieu.zip')" />
```

Vì vậy `dotnet build` vẫn chạy được, nhưng app sẽ không có thư viện nhạc mẫu.

## Tạo payload đầy đủ

Yêu cầu: Node.js 22+ và thư mục dữ liệu `CTECSoundPlayer` nằm cạnh repo (mặc định script tìm `..\..\CTECSoundPlayer`), hoặc đặt biến môi trường `CTEC_SEED_DIR`.

```powershell
node scripts/make-payload.cjs
powershell -ExecutionPolicy Bypass -File scripts/make-payload.ps1
```

Kết quả: `payload/du_lieu.zip` và `payload/manifest.json`. Sau đó build lại app:

```powershell
dotnet publish src/GSoundPlayer.App/GSoundPlayer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

`payload/*.zip` và `payload/manifest.json` đã được `.gitignore` bỏ qua.
