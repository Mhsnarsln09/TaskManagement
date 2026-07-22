# Görev 05 — Yetkilendirme ve iş kuralları

## Amaç

Rol, proje üyeliği ve kaynak sahipliğine bağlı kuralları merkezi ve test edilebilir biçimde uygulamak.

## Neden?

`[Authorize(Roles = "...")]` yalnızca kaba bir kapıdır. Kullanıcının belirli bir projeyi veya görevi görüp görememesi veriye bağlıdır ve her endpoint'te tutarlı kontrol edilmelidir.

## Yapılacaklar

- [x] Proje oluşturucusunu otomatik ProjectMember olarak ekle.
- [x] Proje liste sorgusunu aktif kullanıcının üyelikleriyle sınırla.
- [x] Proje üyesi ekleme/çıkarma kullanım senaryolarını oluştur.
- [x] Aynı üyeliği ikinci kez eklemeyi iş kuralı ve DB constraint ile reddet.
- [x] Göreve atanacak kullanıcının proje üyesi olduğunu doğrula.
- [x] Görev silmeyi proje yöneticisi/Admin ile sınırla.
- [x] Tamamlanmış görev değişikliğinde yeniden açma politikasını uygula.
- [x] İzin verilen durum geçişlerini açıkça tanımla ve test et.
- [x] Kaynak bazlı authorization handler/policy veya merkezi application servisi seçimini yap.
- [x] `401`, `403` ve bilgi sızmasını önlemek için gerektiğinde `404` davranışını dokümante et.
- [x] Gecikme bilgisini `DueDate < UtcNow && Status != Completed` olarak hesapla; zamanı test edebilmek için `TimeProvider` kullan.

## Test senaryoları

> Aşağıdaki silme senaryoları ilk implementasyonun kaydıdır. Hedef MVP yetki matrisi
> [Görev 10 / B10-02](10-mvp-hardening.md) ile değiştirilmiştir.

- Üye olmayan kullanıcı proje listesinden projeyi göremez.
- Üye olmayan kullanıcı doğrudan ID ile görevi okuyamaz.
- Member görev silemez.
- ProjectManager kendi projesindeki görevi silebilir.
- Başka projenin üyesi göreve atanamaz.
- Completed görev kural dışı düzenlenemez.

## Kabul kriterleri

- Kritik yetki kontrolü yalnızca UI varsayımına bırakılmamış.
- Bütün sorgular tenant benzeri proje sınırını koruyor.
- İş kuralları controller'dan bağımsız test edilebiliyor.
- Zaman bağımlı kural sistem saatine doğrudan bağlı değil.

## Önerilen commit

```text
feat(application): enforce project authorization and task rules
```
