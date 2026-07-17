# Görev 03 — Temel proje ve görev API'leri

## Amaç

Authentication eklemeden önce proje ve görev kullanım senaryolarının HTTP sözleşmesini, validation ve veri erişimini kurmak.

## Neden?

Önce basit bir uçtan uca dikey dilim kurmak; controller, application katmanı ve EF Core arasındaki veri akışını görünür kılar. Bu aşamada geçici kullanıcı kimliği yalnızca development kolaylığıdır ve sonraki görevde kaldırılır.

## Yapılacaklar

- [x] Entity'leri dışarı açmayan request/response DTO'ları tasarla.
- [x] Proje create, list, detail, update ve delete kullanım senaryolarını oluştur.
- [x] Görev create, list, detail, update ve delete kullanım senaryolarını oluştur.
- [x] Controller'ları ince tut; iş kuralını controller içine yazma.
- [x] FluentValidation ile başlık, açıklama, tarih, status ve priority girdilerini doğrula.
- [x] Liste sorgusuna `page`, `pageSize`, `status`, `priority`, `sortBy`, `sortDirection` ekle.
- [x] Maksimum page size belirle.
- [x] Query sonuçlarında entity yükleyip sonra map etmek yerine uygun yerlerde projection kullan.
- [x] Global exception handler ve RFC Problem Details hata formatını kur.
- [x] Swagger response ve örneklerini anlamlı hale getir.

## HTTP davranışı

- Oluşturma: `201 Created` ve kaynak konumu
- Başarılı okuma/güncelleme: `200 OK`
- Başarılı silme: `204 No Content`
- Geçersiz girdi: `400 Bad Request`
- Kimlik yok/geçersiz: `401 Unauthorized`
- Kimlik var fakat yetki yok: `403 Forbidden`
- Kaynak yok: `404 Not Found`
- İş kuralı çakışması: uygun durumda `409 Conflict`

## Kabul kriterleri

- DTO ile entity ayrımı korunuyor.
- Filtreli ve sayfalı görev listesi deterministik sıralama kullanıyor.
- Hatalar ortak Problem Details sözleşmesine sahip.
- Controller yalnızca HTTP sorumluluklarını taşıyor.
- CRUD akışı Swagger üzerinden doğrulanabiliyor.

## Önerilen commit

```text
feat(api): add project and task CRUD endpoints
```
