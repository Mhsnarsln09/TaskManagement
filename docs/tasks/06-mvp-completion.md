# Görev 06 — Yorum, dosya ve istatistik

## Amaç

Ekip iş birliği ve proje görünürlüğü için kalan MVP özelliklerini tamamlamak.

## Neden?

Yorumlar bağlamı, dosyalar iş çıktısını, istatistikler ise ilerleme bilgisini sağlar. Dosya yükleme özellikle güvenlik sınırlarını öğrenmek için önemlidir.

## Yapılacaklar

### Yorum

- [ ] Proje üyesinin göreve yorum eklemesini sağla.
- [ ] Yorumları oluşturma tarihine göre sayfalı listele.
- [ ] Yazar bilgisinin güvenli özetini response'a ekle.
- [ ] Yorum düzenleme/silme MVP kapsamını netleştir; yoksa endpoint ekleme.

### Dosya

- [ ] `Attachment` entity/metadata modelini ekle ve migration üret.
- [ ] İzin verilen maksimum boyut ve uzantıları configuration'a taşı.
- [ ] İstemci dosya adını fiziksel dosya adı olarak kullanma; rastgele isim üret.
- [ ] Path traversal, MIME/uzantı ve boş dosya kontrollerini uygula.
- [ ] Depolama davranışını `IFileStorage` arayüzü arkasına al; ilk implementasyon yerel olabilir.
- [ ] Dosya metadata kaydı ile fiziksel yazma başarısızlıklarının tutarlılığını ele al.
- [ ] Yetkili indirme endpoint'i oluştur.

### İstatistik

- [ ] Toplam, Todo, InProgress, Completed ve overdue sayılarını tek projection/query ile hesaplamayı değerlendir.
- [ ] Tamamlanma yüzdesinde sıfıra bölmeyi yönet.
- [ ] İstatistik endpoint'ini yalnızca proje üyelerine aç.

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

