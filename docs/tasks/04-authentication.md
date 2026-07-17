# Görev 04 — Identity ve JWT

## Amaç

Kullanıcı kimliğini güvenli biçimde doğrulamak ve API'ye stateless erişim sağlamak.

## Neden?

Authentication ve authorization farklı problemlerdir. Identity parola hashleme ve kullanıcı yönetimini güvenli şekilde sağlar; JWT ise her istekte kullanıcının kimliğini API'ye taşır.

## Yapılacaklar

- [x] `ApplicationUser` modelini ASP.NET Core Identity ile oluştur.
- [x] Identity tablolarını DbContext'e ekleyen migration üret.
- [x] Register request ve validation oluştur.
- [x] Login request ve parola doğrulaması oluştur.
- [x] Güçlü JWT signing key'i user-secrets/environment variable üzerinden al.
- [x] Issuer, audience, expiry ve clock skew ayarlarını yap.
- [x] Token'a minimum claim setini ekle: subject/user id, email/username ve role.
- [x] `Admin`, `ProjectManager`, `Member` rollerini idempotent biçimde seed et.
- [x] Swagger'a Bearer authentication ekle.
- [x] Controller'lardaki geçici kullanıcı kimliğini `ClaimsPrincipal` üzerinden alınan kimlikle değiştir.
- [x] Hatalı login'de kullanıcı var/yok bilgisini sızdırmayan cevap dön.

## Güvenlik notları

- Parolayı hiçbir zaman kendin hashleme veya loglama.
- JWT içine hassas bilgi koyma; JWT imzalıdır ama varsayılan olarak şifreli değildir.
- Access token ömrünü sınırlı tut. Refresh token MVP sonrasıdır.
- Role claim tek başına proje üyeliğini kanıtlamaz.

## Kabul kriterleri

- Kayıt olan kullanıcının parolası düz metin saklanmıyor.
- Geçerli login JWT üretiyor; hatalı login üretmiyor.
- Token olmadan korumalı endpoint `401` dönüyor.
- Swagger Bearer token ile korumalı çağrı yapabiliyor.
- JWT secret repoda bulunmuyor.

## Önerilen commit

```text
feat(auth): add identity registration login and jwt
```
