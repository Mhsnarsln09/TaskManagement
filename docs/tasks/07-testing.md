# Görev 07 — Testler ve kalite kapıları

## Amaç

Kritik davranışları hızlı unit testlerle ve gerçek altyapıya yakın integration testlerle güvenceye almak.

## Neden?

Mock kullanan testler iş kararlarını hızlı sınar; integration testleri ise routing, middleware, serialization, authorization ve EF mapping gibi parçaların birlikte çalıştığını kanıtlar. İkisi farklı riskleri yakalar.

## Yapılacaklar

- [x] xUnit test projelerini ana solution'a bağla. (`TaskManagement.UnitTests` + `TaskManagement.IntegrationTests` solution'da.)
- [x] NSubstitute veya Moq'dan birini seç; ikisini birden kullanma. (Karar: NSubstitute — bkz. TECHNICAL-DECISIONS.md.)
- [x] Domain durum geçişi testlerini tamamla. (`TaskItemTests`: start/complete/cancel/reopen ve edit kilitleri.)
- [x] Görev oluşturma validation ve üyelik testlerini yaz. (`TaskServiceTests` mock'lu + `ValidationTests`/`BusinessRuleTests` uçtan uca.)
- [x] Tamamlanmış görev düzenleme testlerini yaz. (Reopen politikası: `TaskServiceTests` + `BusinessRuleTests`.)
- [x] Yetkilendirme handler/service testlerini yaz. (`ProjectAuthorizationServiceTests`: üyelik/sahiplik/rol kapısı/Admin bypass.)
- [x] `WebApplicationFactory` ile API test host'u kur. (`TaskManagementApiFactory`.)
- [x] Testcontainers ile gerçek PostgreSQL container'ı başlat. (`Testcontainers.PostgreSql`, `postgres:18`, `Database.MigrateAsync()` ile şema.)
- [x] Her integration test grubu için izole veri stratejisi belirle. (Sınıf başına `IClassFixture` → kendi container'ı + taze veritabanı; testler kendi verisini benzersiz adlarla üretir.)
- [x] Register/login ve JWT korumalı endpoint akışını test et. (`AuthenticationFlowTests`: login → korumalı endpoint, yanlış şifre, token yok/tahrif edilmiş.)
- [x] Proje izolasyonu ve `401/403/404` davranışlarını test et. (`BusinessRuleTests` + `ValidationTests`.)
- [x] Dosya yüklemede boyut/tür ve yetki testleri yaz. (`MvpCompletionTests`: uzantı, boş dosya, traversal, üye olmayan.)
- [x] CI için `dotnet restore`, `build --no-restore`, `test --no-build` sırasını hazırla. (`.github/workflows/ci.yml`.)

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

