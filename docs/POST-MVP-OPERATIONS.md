# Post-MVP Operasyon Rehberi

## Gözlemlenebilirlik

Her HTTP isteği `X-Correlation-ID` taşır. İstemci en fazla 100 karakterlik bir değer
gönderirse korunur, aksi halde API üretir ve response header'ında döndürür. Serilog
request logları method, path, status, süre ve correlation id içerir. Request body,
JWT, parola, refresh token, e-posta adresi ve dosya içeriği loglanmaz.

`/health/live` yalnızca process'in yaşadığını, `/health/ready` PostgreSQL
bağlantısını denetler. Orchestrator trafik vermeden önce readiness sonucunu
kullanmalıdır.

Audit trail EF Core `SaveChanges` sınırında Added/Modified/Deleted olaylarını;
entity tipi, entity id, kullanıcı id, correlation id ve UTC zamanıyla kaydeder.
Alanların eski/yeni değerleri kişisel veri sızıntısı ve gereksiz veri çoğalması
oluşturmamak için tutulmaz. Audit tablosuna uygulama endpoint'i açılmamıştır.

## Background Job ve Bildirim

Hangfire her gün 07:00 UTC'de ertesi gün son tarihi gelen açık görevleri tarar.
`due:{taskId}:{dueDate}` anahtarı aynı reminder'ın ikinci kez üretilmesini engeller.
Job eşzamanlı çalışamaz ve başarısız olduğunda 60, 300 ve 900 saniye sonra retry
edilir. Development e-posta adapterı içerik ve alıcıyı loglamadan teslimatı simüle
eder; production providerı `IEmailSender` implementasyonu olarak eklenmelidir.

Bildirimler PostgreSQL'de kalıcıdır. Kullanıcı `/api/notifications` ile sayfalı
listeyi okur ve `PUT /api/notifications/{id}/read` ile okundu işaretler. SignalR
`/hubs/notifications` üzerinden `notificationReceived` olayı yollar. Realtime
teslimat başarısızlığı kalıcı iş işlemini geri almaz.

## Performans ve Cache

EF sorguları read endpointlerinde projection ve `AsNoTracking` kullanır. Reminder
sorgusu için `(ProjectId, Status, DueDate)` bileşik indeksi eklenmiştir. PostgreSQL
slow-query ölçümü için production'da `log_min_duration_statement` önce 500 ms ile
başlatılmalı; indeks ekleme kararı gerçek execution plan ile verilmelidir.

Yalnızca proje istatistiği 30 saniye Redis'te cache'lenir. Görev create/update/delete
işlemleri ilgili key'i siler. `ConnectionStrings:Redis` yoksa `NoOpApplicationCache`
devreye girer; sistem cache olmadan aynı doğrulukta çalışır.

Global rate limit kullanıcı adı veya IP başına dakikada 120 istektir ve aşımda 429
döner. Login/register için ayrı, daha sıkı dağıtık limit üretim trafiği ölçüldüğünde
eklenmelidir.

## Güvenlik ve Veri Yaşam Döngüsü

Refresh tokenlar 14 gün geçerlidir, her kullanımda döndürülür ve veritabanında
yalnızca SHA-256 özeti tutulur. Kullanılmış token tekrar sunulursa token ailesinin
tümü iptal edilir. Project ve TaskItem silme işlemleri soft delete'tir; global query
filter silinen kayıtları normal sorgulardan gizler. Fiziksel temizleme ayrı retention
job'ı olmadan yapılmamalıdır.

Dosya yüklemeleri metadata yazılmadan önce malware scanner portundan geçer. Mevcut
adapter EICAR test imzasını reddeden temel korumadır; production'da `IFileScanner`
ClamAV veya seçilen tarama servisine bağlanmalıdır. `IFileStorage` object storage'a
geçiş sınırıdır. Provider, bucket politikası ve veri bölgesi kararı verilmediği için
yerel adapter korunmuştur.

## Deployment ve Geri Dönüş

`Dockerfile` multi-stage publish üretir ve root olmayan `app` kullanıcısıyla çalışır.
Compose API, PostgreSQL ve Redis'i health dependency ile başlatır. Secretlar image'a
yazılmaz; environment veya secret store üzerinden verilir.

Production migration sırası:

1. Veritabanı yedeği al ve mevcut migration id'sini kaydet.
2. Yeni image ile ayrı bir migration job çalıştır.
3. `/health/ready` başarılı olduktan sonra trafiği yeni sürüme geçir.
4. Hata halinde önce eski image'a dön. Migration geriye uyumluysa şemayı ileri halde bırak.
5. Şema geri dönüşü zorunluysa önceki migration hedefiyle `dotnet ef database update <PreviousMigration>` çalıştır ve veri kaybı etkisini onayla.

Compose kolaylığı için `Database__MigrateOnStartup=true` kullanır. Çok instance'lı
production ortamında bu ayar `false` olmalı ve migration yalnızca tek deployment job
tarafından çalıştırılmalıdır. Feature kapatma noktaları `BackgroundJobs:Enabled`,
Redis connection string'inin kaldırılması ve eski container image'a rollback'tir.
