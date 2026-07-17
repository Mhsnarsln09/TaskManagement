# Görev 07 — Testler ve kalite kapıları

## Amaç

Kritik davranışları hızlı unit testlerle ve gerçek altyapıya yakın integration testlerle güvenceye almak.

## Neden?

Mock kullanan testler iş kararlarını hızlı sınar; integration testleri ise routing, middleware, serialization, authorization ve EF mapping gibi parçaların birlikte çalıştığını kanıtlar. İkisi farklı riskleri yakalar.

## Yapılacaklar

- [ ] xUnit test projelerini ana solution'a bağla.
- [ ] NSubstitute veya Moq'dan birini seç; ikisini birden kullanma.
- [ ] Domain durum geçişi testlerini tamamla.
- [ ] Görev oluşturma validation ve üyelik testlerini yaz.
- [ ] Tamamlanmış görev düzenleme testlerini yaz.
- [ ] Yetkilendirme handler/service testlerini yaz.
- [ ] `WebApplicationFactory` ile API test host'u kur.
- [ ] Testcontainers ile gerçek PostgreSQL container'ı başlat.
- [ ] Her integration test grubu için izole veri stratejisi belirle.
- [ ] Register/login ve JWT korumalı endpoint akışını test et.
- [ ] Proje izolasyonu ve `401/403/404` davranışlarını test et.
- [ ] Dosya yüklemede boyut/tür ve yetki testleri yaz.
- [ ] CI için `dotnet restore`, `build --no-restore`, `test --no-build` sırasını hazırla.

## Test isimlendirme örneği

```text
MethodName_StateUnderTest_ExpectedBehavior
CreateTask_AssigneeIsNotProjectMember_ReturnsConflict
DeleteTask_UserIsMemberButNotManager_ReturnsForbidden
ChangeStatus_TaskIsCompleted_RejectsInvalidTransition
```

## Kaçınılacak hatalar

- EF Core InMemory provider ile ilişkisel veritabanı davranışını kanıtladığını varsayma.
- Her sınıfı mock'layıp implementasyon ayrıntısını test etme.
- Sadece başarı senaryosu yazma.
- Testlerin ortak sıraya veya önceden var olan veriye bağlı olmasına izin verme.

## Kabul kriterleri

- Kritik üç alanın olumlu ve olumsuz testleri var: görev oluşturma, yetkilendirme, durum değiştirme.
- Integration testleri gerçek PostgreSQL üzerinde migration uyguluyor.
- Testler birbirinden bağımsız ve tekrar çalıştırılabilir.
- `dotnet test` tek komutla tüm suite'i çalıştırıyor.

## Önerilen commit

```text
test: cover critical task and authorization workflows
```

