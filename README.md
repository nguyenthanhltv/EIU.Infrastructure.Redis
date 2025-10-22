# 🧠 EIU.Infrastructure.Redis

Thư viện trung gian (library) cung cấp cơ chế **Redis Caching**, **Interceptor** và **Attribute-based caching** cho các dự án .NET, giúp dễ dàng quản lý cache, tăng hiệu năng và giảm truy vấn dư thừa.

---

## 📂 Cấu trúc thư mục

```
EIU.Infrastructure.Redis/
│
├── Attributes/
│   ├── RedisCacheAttribute.cs              # Attribute để cache kết quả của phương thức
│   ├── RedisCacheRemoveAttribute.cs        # Attribute để xóa cache theo key sau khi thực thi
│
├── Core/
│   ├── IRedisCacheService.cs               # Interface định nghĩa contract cho Redis cache service
│   ├── RedisCacheService.cs                # Cài đặt thao tác cơ bản với Redis
│   ├── IRedisKeyBuilder.cs                 # Interface định nghĩa builder tạo key động
│   ├── RedisKeyBuilder.cs                  # Cài đặt builder tạo key Redis dựa vào metadata
│
├── Manager/
│   ├── IRedisCacheManager.cs               # Interface quản lý các thao tác cache cấp cao
│   ├── RedisCacheManager.cs                # Triển khai quản lý cache
│
├── Interceptors/
│   ├── RedisCacheInterceptor.cs            # Interceptor xử lý logic cache động
│
├── Extensions/
│   ├── RedisServiceCollectionExtensions.cs # Cấu hình DI cho Redis
│
└── EIU.Infrastructure.Redis.csproj
```

---

## ⚙️ Cài đặt

### 1️⃣ Tạo gói NuGet
```bash
dotnet pack -c Release
```

Sau khi build thành công, file `.nupkg` sẽ nằm trong thư mục:
```
bin/Release/
```

### 2️⃣ Thêm vào project khác
Bạn có thể thêm trực tiếp thông qua đường dẫn gói cục bộ:

```bash
dotnet add package EIU.Infrastructure.Redis --source "path/to/bin/Release"
```

Hoặc chỉnh `NuGet.config` của project để trỏ về thư mục chứa `.nupkg`.

---

## 🚀 Cách sử dụng

### 1️⃣ Đăng ký trong `Program.cs`

```csharp
using EIU.Infrastructure.Redis.Extensions;

builder.Services.AddRedisInfrastructure(builder.Configuration);
```

### 2️⃣ Sử dụng trong Service hoặc Controller

#### ✅ Cache kết quả phương thức
```csharp
[RedisCache("GetUser_{id}", ExpirationInSeconds = 3600)]
public async Task<UserDto> GetUserByIdAsync(int id)
{
    return await _repository.GetByIdAsync(id);
}
```

#### ❌ Xóa cache sau khi cập nhật dữ liệu
```csharp
[RedisCacheRemove("GetUser_{id}")]
public async Task UpdateUserAsync(int id, UserDto user)
{
    await _repository.UpdateAsync(id, user);
}
```

### 3️⃣ Sử dụng cache trực tiếp trong service

```csharp
public class UserService
{
    private readonly IRedisCacheManager _cacheManager;

    public UserService(IRedisCacheManager cacheManager)
    {
        _cacheManager = cacheManager;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _cacheManager.GetOrSetAsync("AllUsers", async () =>
            await _repository.GetAllAsync(),
            TimeSpan.FromMinutes(30));
    }
}
```

---

## 🧩 Yêu cầu

- .NET 8 trở lên
- Redis server hoạt động
- Các gói NuGet:
  - `StackExchange.Redis`
  - `Scrutor`
  - `Castle.Core`
  - `Microsoft.Extensions.DependencyInjection.Abstractions`

---

## 👨‍💻 Tác giả
Phát triển bởi **EIU Dev Team**

---

## 🧩 License

MIT License © 2025 EIU Developer Team


