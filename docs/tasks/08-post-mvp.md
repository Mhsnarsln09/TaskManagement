# Görev 08 — MVP sonrası profesyonelleştirme

## Amaç

Çalışan ve test edilmiş MVP'yi gözlemlenebilir, ölçeklenebilir ve dağıtılabilir hale getirmek.

## Sıralama ilkesi

Bu maddeler toplu halde eklenmemelidir. Her biri ölçülen veya açıkça tanımlanmış bir ihtiyaca cevap vermeli ve ayrı teknik görev olarak ele alınmalıdır.

## Önerilen sıra

### 1. Gözlemlenebilirlik ve hata yönetimi

- [ ] Serilog yapılandırılmış loglar ve correlation id
- [ ] Health checks
- [ ] Audit log için hangi olayların tutulacağı
- [ ] Loglarda kişisel/hassas veri temizliği

### 2. Kod organizasyonu

- [ ] Karmaşıklaşan use-case'leri command/query olarak ayır
- [ ] Gerçek ihtiyaç varsa MediatR pipeline davranışları ekle
- [ ] Beklenen iş hataları için tutarlı Result pattern tasarla
- [ ] Pagination/filter/sort sözleşmesini ortaklaştır

CQRS, her endpoint için iki klasör açmak değildir. Okuma ve yazma modelleri farklılaştığında veya davranış pipeline'ına ihtiyaç olduğunda değer üretir.

### 3. Arka plan işlemleri ve bildirim

- [ ] Hangfire ile son tarih hatırlatma işi
- [ ] E-posta provider abstraction ve retry/idempotency
- [ ] SignalR ile görev atama/durum değişikliği bildirimleri
- [ ] Notification kayıt modeli ve okunma durumu

### 4. Performans

- [ ] Önce ölçüm ve yavaş sorgu analizi
- [ ] Doğru projection ve indeks optimizasyonu
- [ ] Yalnızca uygun okuma verileri için Redis cache
- [ ] Cache invalidation stratejisi
- [ ] Rate limiting politikaları

### 5. Veri yaşam döngüsü ve güvenlik

- [ ] Refresh token rotation ve reuse detection
- [ ] Soft delete ve global query filter etkileri
- [ ] Optimistic concurrency
- [ ] Audit trail
- [ ] Dosyaları object storage'a taşıma ve malware taraması

### 6. Teslimat

- [ ] Multi-stage Dockerfile
- [ ] API, PostgreSQL ve gerekirse Redis için Compose
- [ ] GitHub Actions build/test pipeline
- [ ] Migration deployment stratejisi
- [ ] Environment bazlı secret yönetimi
- [ ] Production health/readiness ve rollback planı

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

