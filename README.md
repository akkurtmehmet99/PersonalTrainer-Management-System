# Personal Trainer Management System

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-8-512BD4)](https://learn.microsoft.com/ef/core/)
[![SQLite](https://img.shields.io/badge/Database-SQLite-003B57?logo=sqlite)](https://www.sqlite.org/)

Personal Trainer Management System; eğitmenleri, danışanları ve bire bir antrenman randevularını yönetmek için geliştirilmiş bir ASP.NET Core Web API projesidir.

**GitHub:** [akkurtmehmet99/PersonalTrainer-Management-System](https://github.com/akkurtmehmet99/PersonalTrainer-Management-System)

## Özellikler

- Eğitmen, danışan ve randevular için tam CRUD REST API
- Entity Framework Core 8 ve SQLite ile kalıcı veri saklama
- İstek ve yanıt modellerinde DTO kullanımı ve Data Annotations doğrulaması
- Randevu durumu, tarih aralığı, eğitmen ve danışana göre filtreleme
- Aynı eğitmen veya danışan için çakışan planlı randevuların engellenmesi
- Eğitmen-danışan atamasının ve e-posta benzersizliğinin korunması
- `application/problem+json` biçiminde merkezi, trace ID içeren hata yanıtları
- Swagger UI ile etkileşimli API dokümantasyonu
- İlk çalıştırmada SQLite veritabanı ve şemasının otomatik oluşturulması

## Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Çalıştırma

```bash
git clone https://github.com/akkurtmehmet99/PersonalTrainer-Management-System.git
cd PersonalTrainer-Management-System
dotnet restore TrainerClientManagement.API.sln
dotnet run --project TrainerClientManagement.API/TrainerClientManagement.API.csproj
```

Uygulama başlangıçta `trainer-client-management.db` SQLite dosyasını çalışma dizininde oluşturur. Bağlantı dizesi `TrainerClientManagement.API/appsettings.json` içindeki `ConnectionStrings:DefaultConnection` ile değiştirilebilir. Uygulamayı başlattıktan sonra terminalde gösterilen adrese giderek Swagger UI'ı açın.

> Veritabanı şeması uygulama ilk açıldığında oluşturulur. Mevcut veritabanlarında şema değişiklikleri için migration tabanlı güncelleme gerekir; `EnsureCreated` mevcut şemayı yükseltmez.
>
> Önceki SQL Server verileri SQLite'a otomatik aktarılmaz. Var olan verileriniz varsa SQLite'a geçmeden önce ayrıca dışa aktarıp taşıyın.

## API uç noktaları

| Kaynak | Yöntem | Uç nokta | Açıklama |
|---|---|---|---|
| Eğitmen | `GET` | `/api/trainers` | Eğitmenleri listele |
| Eğitmen | `GET` | `/api/trainers/{id}` | Eğitmen detayını getir |
| Eğitmen | `POST` | `/api/trainers` | Eğitmen oluştur |
| Eğitmen | `PUT` | `/api/trainers/{id}` | Eğitmeni güncelle |
| Eğitmen | `DELETE` | `/api/trainers/{id}` | Bağlı kaydı olmayan eğitmeni sil |
| Danışan | `GET` | `/api/clients?trainerId=1` | Danışanları, isteğe bağlı eğitmen filtresiyle listele |
| Danışan | `GET` | `/api/clients/{id}` | Danışan detayını getir |
| Danışan | `POST` | `/api/clients` | Danışan oluştur |
| Danışan | `PUT` | `/api/clients/{id}` | Danışanı güncelle |
| Danışan | `DELETE` | `/api/clients/{id}` | Randevusu olmayan danışanı sil |
| Randevu | `GET` | `/api/appointments` | Randevuları filtrele |
| Randevu | `GET` | `/api/appointments/{id}` | Randevu detayını getir |
| Randevu | `POST` | `/api/appointments` | Randevu oluştur |
| Randevu | `PUT` | `/api/appointments/{id}` | Randevuyu güncelle |
| Randevu | `DELETE` | `/api/appointments/{id}` | Randevuyu sil |

Randevu listesi `trainerId`, `clientId`, `status`, `from` ve `to` sorgu parametrelerini destekler. Durum değerleri `Scheduled`, `Completed` ve `Cancelled` şeklindedir. Planlı randevularda süre 15–240 dakika arasında olabilir; eğitmen veya danışanın zaman aralıkları çakışamaz.

### Örnek istekler

```http
POST /api/trainers
Content-Type: application/json

{
  "name": "Ayşe Yılmaz",
  "specialization": "Strength Training",
  "email": "ayse@example.com"
}
```

```http
POST /api/clients
Content-Type: application/json

{
  "name": "Mehmet Demir",
  "email": "mehmet@example.com",
  "phone": "+90 555 123 45 67",
  "trainerId": 1
}
```

```http
POST /api/appointments
Content-Type: application/json

{
  "appointmentDate": "2026-11-01T10:00:00Z",
  "durationMinutes": 60,
  "status": "Scheduled",
  "trainerId": 1,
  "clientId": 1
}
```

## Yanıtlar ve hatalar

- Başarılı oluşturma: `201 Created` ve kaynağa `Location` başlığı
- Başarılı silme/güncelleme: `204 No Content`
- Doğrulama hataları: `400 Bad Request` ve alan bazlı `ValidationProblemDetails`
- Bulunamayan kaynak: `404 Not Found`
- E-posta veya randevu çakışmaları ve kısıt ihlalleri: `409 Conflict`
- Beklenmeyen hatalar: genel detay içermeyen `500` ProblemDetails yanıtı ve destek için `traceId`

## Teknoloji yığını

- ASP.NET Core Web API (.NET 8)
- Entity Framework Core 8
- SQLite
- DTO tabanlı istek/yanıt modelleri
- Swagger / OpenAPI

## Lisans

Bu proje [GitHub deposunda](https://github.com/akkurtmehmet99/PersonalTrainer-Management-System) geliştirilmekte ve paylaşılmaktadır.
