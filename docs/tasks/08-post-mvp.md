# Görev 08 — MVP sonrası profesyonelleştirme

## Amaç

Çalışan ve test edilmiş MVP'yi gözlemlenebilir, ölçeklenebilir ve dağıtılabilir hale getirmek.

## Sıralama ilkesi

Bu maddeler toplu halde eklenmemelidir. Her biri ölçülen veya açıkça tanımlanmış bir ihtiyaca cevap vermeli ve ayrı teknik görev olarak ele alınmalıdır.

## Önerilen sıra

### 1. Gözlemlenebilirlik ve hata yönetimi

- [x] Serilog yapılandırılmış loglar ve correlation id
- [x] Health checks
- [x] Audit log için hangi olayların tutulacağı
- [x] Loglarda kişisel/hassas veri temizliği

### 2. Kod organizasyonu

- [x] Karmaşıklaşan use-case'leri command/query olarak ayır (değerlendirildi; henüz ayrı read/write model yok)
- [x] Gerçek ihtiyaç varsa MediatR pipeline davranışları ekle (ihtiyaç oluşmadığı için eklenmedi)
- [x] Beklenen iş hataları için tutarlı Result pattern tasarla (ProblemDetails + typed application exception sözleşmesi korundu)
- [x] Pagination/filter/sort sözleşmesini ortaklaştır

CQRS, her endpoint için iki klasör açmak değildir. Okuma ve yazma modelleri farklılaştığında veya davranış pipeline'ına ihtiyaç olduğunda değer üretir.

### 3. Arka plan işlemleri ve bildirim

- [x] Hangfire ile son tarih hatırlatma işi
- [x] E-posta provider abstraction ve retry/idempotency
- [x] SignalR ile görev atama/durum değişikliği bildirimleri
- [x] Notification kayıt modeli ve okunma durumu

### 4. Performans

- [x] Önce ölçüm ve yavaş sorgu analizi
- [x] Doğru projection ve indeks optimizasyonu
- [x] Yalnızca uygun okuma verileri için Redis cache
- [x] Cache invalidation stratejisi
- [x] Rate limiting politikaları

### 5. Veri yaşam döngüsü ve güvenlik

- [x] Refresh token rotation ve reuse detection
- [x] Soft delete ve global query filter etkileri
- [x] Optimistic concurrency
- [x] Audit trail
- [x] Dosyaları object storage'a taşıma ve malware taraması (storage portu hazır; local adapter ve tarama aktif, object-storage adapterı provider seçimine bırakıldı)

### 6. Teslimat

- [x] Multi-stage Dockerfile
- [x] API, PostgreSQL ve gerekirse Redis için Compose
- [x] GitHub Actions build/test pipeline
- [x] Migration deployment stratejisi
- [x] Environment bazlı secret yönetimi
- [x] Production health/readiness ve rollback planı

## Her ileri özellik için görev şablonu

Yeni bir özelliğe başlamadan şu sorular cevaplanmalıdır:

1. Hangi kullanıcı veya operasyon problemi çözülüyor?
2. Başarı nasıl ölçülecek?
3. Veri modeli ve güvenlik etkisi nedir?
4. Başarısızlık ve retry davranışı nedir?
5. Unit/integration test sınırı nedir?
6. Geri alma veya özelliği kapatma yöntemi nedir?

## Nihai kabul kriterleri

- CI her pull request'te build ve test çalıştırıyor.
- Uygulama container ile tekrar üretilebilir biçimde ayağa kalkıyor.
- Kritik işlemler correlation id ile izlenebiliyor.
- Background job'lar tekrar çalıştığında veri bozmuyor.
- Cache kapatıldığında sistem doğru çalışmaya devam ediyor.
- Deployment ve migration geri dönüş planı dokümante edilmiş.

## Uygulama notları

Teknik kararların gerekçesi, operasyon metrikleri ve geri dönüş adımları
`docs/POST-MVP-OPERATIONS.md` içindedir. CQRS/MediatR ile harici object storage
providerı ölçülmüş ihtiyaç veya provider kararı olmadığı için mekanik olarak
eklenmemiştir; ilgili portlar katman sınırlarını değiştirmeden adapter eklemeye hazırdır.
