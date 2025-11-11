# Hướng Dẫn Frontend Gửi JWT Token

## Cấu hình API Client

### 1. Angular/TypeScript (HttpClient)

```typescript
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private apiUrl = 'http://localhost:5000/api';
  
  constructor(private http: HttpClient) {}

  // Lấy token từ localStorage hoặc service
  private getAuthToken(): string | null {
    return localStorage.getItem('token'); // hoặc từ auth service
  }

  // Tạo headers với JWT token
  private getAuthHeaders(): HttpHeaders {
    const token = this.getAuthToken();
    let headers = new HttpHeaders();
    
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }
    
    return headers;
  }

  // Ví dụ: Lấy danh sách roles
  getRoles() {
    return this.http.get(`${this.apiUrl}/roles`, {
      headers: this.getAuthHeaders()
    });
  }
}
```

### 2. Interceptor (Khuyến nghị - Tự động thêm token)

```typescript
import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler } from '@angular/common/http';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler) {
    const token = localStorage.getItem('token');
    
    if (token) {
      const cloned = req.clone({
        headers: req.headers.set('Authorization', `Bearer ${token}`)
      });
      return next.handle(cloned);
    }
    
    return next.handle(req);
  }
}
```

**Đăng ký interceptor trong `app.module.ts`:**
```typescript
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from './auth.interceptor';

providers: [
  {
    provide: HTTP_INTERCEPTORS,
    useClass: AuthInterceptor,
    multi: true
  }
]
```

### 3. React/JavaScript (Fetch API)

```javascript
// Lấy token từ localStorage
const getToken = () => localStorage.getItem('token');

// Fetch với JWT token
const fetchRoles = async () => {
  const token = getToken();
  
  const response = await fetch('http://localhost:5000/api/roles', {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });
  
  if (!response.ok) {
    if (response.status === 401) {
      // Token hết hạn hoặc không hợp lệ
      // Redirect to login
      window.location.href = '/login';
      return;
    }
    throw new Error('Failed to fetch roles');
  }
  
  return await response.json();
};
```

### 4. Axios (React/Vue)

```javascript
import axios from 'axios';

// Tạo axios instance với interceptor
const apiClient = axios.create({
  baseURL: 'http://localhost:5000/api'
});

// Request interceptor - tự động thêm token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor - xử lý 401
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token hết hạn
      localStorage.removeItem('token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// Sử dụng
const getRoles = async () => {
  try {
    const response = await apiClient.get('/roles');
    return response.data;
  } catch (error) {
    console.error('Error fetching roles:', error);
    throw error;
  }
};
```

## Endpoint yêu cầu JWT Token

### Roles API
- `GET /api/roles` - Lấy danh sách roles (yêu cầu token)
- `GET /api/roles/{id}` - Lấy role theo ID (yêu cầu token)
- `POST /api/roles` - Tạo role mới (yêu cầu token + role Admin)
- `PUT /api/roles/{id}` - Cập nhật role (yêu cầu token + role Admin)
- `DELETE /api/roles/{id}` - Xóa role (yêu cầu token + role Admin)

## Format JWT Token

Token được gửi trong header:
```
Authorization: Bearer <token>
```

Ví dụ:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VybmFtZSI6ImFkbWluIiwicm9sZXMiOlsiQWRtaW5pc3RyYXRvciJdLCJleHAiOjE3MDAwMDAwMDB9.xxx
```

## Xử lý lỗi

### 401 Unauthorized
- Token không hợp lệ hoặc hết hạn
- **Hành động:** Redirect user đến trang login

### 403 Forbidden
- Token hợp lệ nhưng không có quyền (thiếu role)
- **Hành động:** Hiển thị thông báo "Bạn không có quyền truy cập"

## Lưu trữ Token

**Khuyến nghị:**
- `localStorage` - Dễ sử dụng, nhưng có nguy cơ XSS
- `sessionStorage` - Tự động xóa khi đóng tab
- `httpOnly cookie` - An toàn nhất (cần cấu hình backend)

## Test với Postman/Thunder Client

1. Đăng nhập để lấy token:
   ```
   POST http://localhost:5000/api/auth/login
   Body: {
     "userName": "admin",
     "password": "Admin@123"
   }
   ```

2. Copy token từ response

3. Gọi API với token:
   ```
   GET http://localhost:5000/api/roles
   Headers:
     Authorization: Bearer <your-token>
   ```

## CORS Configuration

Backend đã được cấu hình để chấp nhận requests từ:
- Origin: `http://localhost:4200` (Angular default)
- Headers: Any (bao gồm Authorization)
- Credentials: Allowed

Nếu frontend chạy ở port khác, cần cập nhật trong `Program.cs`:
```csharp
policy.WithOrigins("http://localhost:4200", "http://localhost:3000")
```

