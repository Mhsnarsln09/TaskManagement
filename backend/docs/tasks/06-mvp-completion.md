# Görev 06 — Yorum, dosya ve istatistik

## Amaç

Ekip iş birliği ve proje görünürlüğü için kalan MVP özelliklerini tamamlamak.

## Neden?

Yorumlar bağlamı, dosyalar iş çıktısını, istatistikler ise ilerleme bilgisini sağlar. Dosya yükleme özellikle güvenlik sınırlarını öğrenmek için önemlidir.

## Yapılacaklar

### Yorum

- [x] Proje üyesinin göreve yorum eklemesini sağla.
- [x] Yorumları oluşturma tarihine göre sayfalı listele.
- [x] Yazar bilgisinin güvenli özetini response'a ekle (`UserSummaryResponse`: id, userName, displayName).
- [x] Yorum düzenleme/silme MVP kapsamını netleştir; yoksa endpoint ekleme. (Karar: MVP dışı, endpoint yok — bkz. TECHNICAL-DECISIONS.md.)

### Dosya

- [x] `Attachment` entity/metadata modelini ekle ve migration üret (`AddAttachments`).
- [x] İzin verilen maksimum boyut ve uzantıları configuration'a taşı (`FileUpload` bölümü).
- [x] İstemci dosya adını fiziksel dosya adı olarak kullanma; rastgele isim üret (`{Guid:N}{uzantı}`).
- [x] Path traversal, MIME/uzantı ve boş dosya kontrollerini uygula (`UploadFileName` + `AttachmentService.ValidateUpload`).
- [x] Depolama davranışını `IFileStorage` arayüzü arkasına al; ilk implementasyon yerel (`LocalFileStorage`).
- [x] Dosya metadata kaydı ile fiziksel yazma başarısızlıklarının tutarlılığını ele al (önce dosya, sonra metadata; başarısızlıkta kompanzasyon silmesi).
- [x] Yetkili indirme endpoint'i oluştur (`GET .../attachments/{id}/content`, attachment + nosniff).

### İstatistik

- [x] Toplam, Todo, InProgress, Completed ve overdue sayılarını tek projection/query ile hesapla (`TaskRepository.GetCountsAsync`).
- [x] Tamamlanma yüzdesinde sıfıra bölmeyi yönet (boş proje %0).
- [x] İstatistik endpoint'ini yalnızca proje üyelerine aç (üye olmayana 404).

## Kabul kriterleri

- Üye olmayan kullanıcı yorum ve dosyalara erişemiyor.
- Zararlı/geçersiz dosya adları güvenli şekilde reddediliyor veya normalize ediliyor.
- İstatistikler farklı görev durumları ve boş proje için doğru.
- Dosya metadata'sı istemcinin verdiği fiziksel yola güvenmiyor.

## Önerilen commitler

```text
feat(comments): add task comment endpoints
feat(files): add secured task attachments
feat(statistics): add project progress summary
```

