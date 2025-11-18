# 📘 Hướng Dẫn Sử Dụng API: Lấy Danh Sách Work Items Theo User Đăng Nhập

## Endpoint: `GET /api/Assignments/my-work-items`

API này dùng để lấy danh sách **Work Items** (công việc) của user đang đăng nhập. API sẽ tự động lấy thông tin user từ JWT token và trả về tất cả work items có `PersonName` khớp với `FullName` hoặc `UserName` của user.

---

## 🔐 Authentication

**Yêu cầu**: Request phải có JWT Token trong header.

```http
Authorization: Bearer {your_jwt_token}
```

---

## 📋 Thông Tin Endpoint

- **Method**: `GET`
- **URL**: `/api/Assignments/my-work-items`
- **Content-Type**: `application/json`
- **Authentication**: Required (Bearer Token)

---

## 📤 Request

### Headers

```http
GET /api/Assignments/my-work-items
Authorization: Bearer {your_jwt_token}
Content-Type: application/json
```

### Request Body

Không có request body.

---

## 📥 Response

### Response 200 OK

Trả về danh sách work items của user đăng nhập, mỗi work item bao gồm thông tin assignment liên quan.

```json
[
  {
    "workItemID": 1,
    "assignmentID": 5,
    "workType": "Core Design",
    "personName": "Nguyễn Văn A",
    "startDate": "2025-11-15T08:00:00.000Z",
    "expectedFinish": "2025-11-20T17:00:00.000Z",
    "actualFinish": null,
    "personConfirmation": false,
    "notes": "Ghi chú về công việc",
    "assignment": {
      "assignmentID": 5,
      "tbkt_ID": "TC: 97/QĐ-HDTV",
      "machineName": "MBA 3 pha 250kVA, 35±2x2.5%/0.4kV Dyn11",
      "standardRequirement": "Io ≤ 2.0% (+30%); Po ≤ 340 W",
      "additionalRequest": "Yêu cầu bổ sung",
      "deliveryDate": "2025-11-29T17:00:00.000Z",
      "designer": "2",
      "teamLeader": "4",
      "filePaths": null,
      "status": 2
    }
  },
  {
    "workItemID": 2,
    "assignmentID": 6,
    "workType": "Core Review",
    "personName": "Nguyễn Văn A",
    "startDate": "2025-11-16T08:00:00.000Z",
    "expectedFinish": "2025-11-21T17:00:00.000Z",
    "actualFinish": "2025-11-20T16:30:00.000Z",
    "personConfirmation": true,
    "notes": "Đã hoàn thành",
    "assignment": {
      "assignmentID": 6,
      "tbkt_ID": "TC: 98/QĐ-HDTV",
      "machineName": "MBA 3 pha 500kVA, 35±2x2.5%/0.4kV Dyn11",
      "standardRequirement": "Io ≤ 2.0% (+30%); Po ≤ 680 W",
      "additionalRequest": null,
      "deliveryDate": "2025-12-05T17:00:00.000Z",
      "designer": "3",
      "teamLeader": "4",
      "filePaths": null,
      "status": 3
    }
  }
]
```

### Các Trường Dữ Liệu

#### WorkItemWithAssignmentDto

| Field | Type | Description |
|-------|------|-------------|
| `workItemID` | `integer` | ID của work item |
| `assignmentID` | `integer` | ID của assignment liên quan |
| `workType` | `string?` | Loại công việc (ví dụ: "Core Design", "Core Review", "Casing Design", etc.) |
| `personName` | `string?` | Tên người thực hiện công việc |
| `startDate` | `DateTime?` | Ngày bắt đầu (ISO 8601 format) |
| `expectedFinish` | `DateTime?` | Ngày dự kiến hoàn thành (ISO 8601 format) |
| `actualFinish` | `DateTime?` | Ngày thực tế hoàn thành (ISO 8601 format) |
| `personConfirmation` | `boolean?` | Xác nhận của người thực hiện (true/false/null) |
| `notes` | `string?` | Ghi chú về công việc |
| `assignment` | `MachineAssignmentDto?` | Thông tin assignment liên quan (null nếu không có) |

#### MachineAssignmentDto (trong assignment)

| Field | Type | Description |
|-------|------|-------------|
| `assignmentID` | `integer` | ID của assignment |
| `tbkt_ID` | `string` | Mã TBKT |
| `machineName` | `string` | Tên máy |
| `standardRequirement` | `string?` | Yêu cầu tiêu chuẩn |
| `additionalRequest` | `string?` | Yêu cầu bổ sung |
| `deliveryDate` | `DateTime?` | Ngày giao hàng |
| `designer` | `string?` | Người thiết kế |
| `teamLeader` | `string?` | Trưởng nhóm |
| `filePaths` | `string?` | Đường dẫn file |
| `status` | `integer` | Trạng thái (1: new, 2: đang xử lý, 3: hoàn thành) |

---

## ❌ Error Responses

### 401 Unauthorized

```json
{
  "error": "Invalid user token"
}
```

**Nguyên nhân**: 
- JWT token không hợp lệ
- Token không có thông tin user ID

### 404 Not Found

```json
{
  "error": "User not found"
}
```

**Nguyên nhân**: User ID trong token không tồn tại trong database.

### 500 Internal Server Error

```json
{
  "error": "Error retrieving work items",
  "message": "Chi tiết lỗi..."
}
```

**Nguyên nhân**: Lỗi server khi xử lý request.

---

## 🔍 Cách Hoạt Động

1. API lấy `UserId` từ JWT token (từ claim `sub` hoặc `NameIdentifier`)
2. Tìm user trong database theo `UserId`
3. Lấy `FullName`, `UserName`, và `UserId` (chuyển sang string) của user
4. Tìm tất cả work items có `PersonName` khớp với một trong các giá trị sau:
   - `UserId` (dạng string, ví dụ: "2", "4", "12")
   - `FullName` (ví dụ: "Nguyễn Văn A")
   - `UserName` (ví dụ: "nguyenvana")
5. Sắp xếp theo `StartDate` giảm dần (mới nhất trước)
6. Trả về danh sách work items kèm thông tin assignment

**Lưu ý**: `PersonName` trong database có thể được lưu dưới dạng UserId (số dạng string) hoặc tên người, nên API sẽ kiểm tra cả ba trường hợp để đảm bảo tìm được tất cả work items của user.

---

## 💻 Ví Dụ Sử Dụng Frontend

### JavaScript/TypeScript

```javascript
// Hàm lấy danh sách work items của user đăng nhập
async function getMyWorkItems() {
  try {
    // Lấy token từ localStorage hoặc state
    const token = localStorage.getItem('token');
    
    if (!token) {
      throw new Error('Please login first');
    }

    const response = await fetch('http://localhost:5000/api/assignments/my-work-items', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to get work items');
    }

    const workItems = await response.json();
    console.log('My work items:', workItems);
    return workItems;
  } catch (error) {
    console.error('Get my work items error:', error);
    throw error;
  }
}

// Sử dụng
(async () => {
  try {
    const myWorkItems = await getMyWorkItems();
    
    // Hiển thị danh sách công việc
    myWorkItems.forEach(item => {
      console.log(`- ${item.workType}: ${item.assignment?.machineName}`);
      console.log(`  Trạng thái: ${item.actualFinish ? 'Đã hoàn thành' : 'Đang làm'}`);
      console.log(`  Assignment: ${item.assignment?.tbkt_ID}`);
    });
  } catch (error) {
    console.error('Error:', error);
  }
})();
```

### React Example

```jsx
import { useState, useEffect } from 'react';

function MyWorkItems() {
  const [workItems, setWorkItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    async function fetchWorkItems() {
      try {
        const token = localStorage.getItem('token');
        const response = await fetch('http://localhost:5000/api/assignments/my-work-items', {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
          }
        });

        if (!response.ok) {
          throw new Error('Failed to fetch work items');
        }

        const data = await response.json();
        setWorkItems(data);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    }

    fetchWorkItems();
  }, []);

  if (loading) return <div>Đang tải...</div>;
  if (error) return <div>Lỗi: {error}</div>;

  return (
    <div>
      <h2>Danh Sách Công Việc Của Tôi</h2>
      {workItems.length === 0 ? (
        <p>Bạn chưa có công việc nào.</p>
      ) : (
        <ul>
          {workItems.map(item => (
            <li key={item.workItemID}>
              <h3>{item.workType}</h3>
              <p>Assignment: {item.assignment?.machineName}</p>
              <p>TBKT: {item.assignment?.tbkt_ID}</p>
              <p>Ngày bắt đầu: {item.startDate ? new Date(item.startDate).toLocaleDateString('vi-VN') : 'Chưa có'}</p>
              <p>Dự kiến hoàn thành: {item.expectedFinish ? new Date(item.expectedFinish).toLocaleDateString('vi-VN') : 'Chưa có'}</p>
              <p>Trạng thái: {item.actualFinish ? 'Đã hoàn thành' : 'Đang làm'}</p>
              {item.notes && <p>Ghi chú: {item.notes}</p>}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default MyWorkItems;
```

### Vue.js Example

```vue
<template>
  <div>
    <h2>Danh Sách Công Việc Của Tôi</h2>
    <div v-if="loading">Đang tải...</div>
    <div v-else-if="error">Lỗi: {{ error }}</div>
    <div v-else>
      <div v-if="workItems.length === 0">
        <p>Bạn chưa có công việc nào.</p>
      </div>
      <div v-else>
        <div v-for="item in workItems" :key="item.workItemID" class="work-item">
          <h3>{{ item.workType }}</h3>
          <p>Assignment: {{ item.assignment?.machineName }}</p>
          <p>TBKT: {{ item.assignment?.tbkt_ID }}</p>
          <p>Ngày bắt đầu: {{ formatDate(item.startDate) }}</p>
          <p>Dự kiến hoàn thành: {{ formatDate(item.expectedFinish) }}</p>
          <p>Trạng thái: {{ item.actualFinish ? 'Đã hoàn thành' : 'Đang làm' }}</p>
          <p v-if="item.notes">Ghi chú: {{ item.notes }}</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  data() {
    return {
      workItems: [],
      loading: true,
      error: null
    };
  },
  async mounted() {
    await this.fetchWorkItems();
  },
  methods: {
    async fetchWorkItems() {
      try {
        const token = localStorage.getItem('token');
        const response = await fetch('http://localhost:5000/api/assignments/my-work-items', {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
          }
        });

        if (!response.ok) {
          throw new Error('Failed to fetch work items');
        }

        const data = await response.json();
        this.workItems = data;
      } catch (err) {
        this.error = err.message;
      } finally {
        this.loading = false;
      }
    },
    formatDate(dateString) {
      if (!dateString) return 'Chưa có';
      return new Date(dateString).toLocaleDateString('vi-VN');
    }
  }
};
</script>
```

---

## 📝 Lưu Ý

1. **PersonName Matching**: API sẽ tìm work items có `PersonName` khớp với `FullName` hoặc `UserName` của user. Đảm bảo khi tạo work item, `PersonName` phải khớp với một trong hai giá trị này.

2. **Sorting**: Work items được sắp xếp theo `StartDate` giảm dần (mới nhất trước). Nếu `StartDate` là null, sẽ được đặt ở cuối danh sách.

3. **Assignment Information**: Mỗi work item sẽ kèm theo thông tin assignment đầy đủ để frontend có thể hiển thị ngay mà không cần gọi thêm API.

4. **Empty Result**: Nếu user chưa có work items nào, API sẽ trả về mảng rỗng `[]`.

---

## 🔄 Workflow Điển Hình

1. **User đăng nhập** → Nhận JWT token
2. **Gọi API này** → Lấy danh sách work items của user
3. **Hiển thị danh sách** → Frontend hiển thị công việc của user
4. **User xem chi tiết** → Click vào work item để xem thông tin assignment đầy đủ

---

## 📚 Tài Liệu Liên Quan

- [API Assignments Guide](./API_Assignments_Guide.md) - Hướng dẫn đầy đủ về Assignments API
- [API Work Items Guide](./API_WorkItems_Guide.md) - Hướng dẫn tạo work items
- [API Documentation](./API_Documentation.md) - Tài liệu API tổng quan
- [Frontend API Examples](./FRONTEND_API_EXAMPLES.js) - Ví dụ code frontend

---

## ❓ Câu Hỏi Thường Gặp (FAQ)

**Q: Làm sao để đảm bảo work items được gán đúng cho user?**  
A: Khi tạo work item, `PersonName` có thể là:
- UserId (dạng string, ví dụ: "2", "4", "12") - **Khuyến nghị sử dụng cách này**
- FullName của user (ví dụ: "Nguyễn Văn A")
- UserName của user (ví dụ: "nguyenvana")

API sẽ tự động match với cả ba trường hợp, nhưng khuyến nghị sử dụng UserId để đảm bảo chính xác nhất.

**Q: API có hỗ trợ filter theo trạng thái hông?**  
A: Hiện tại chưa có. API trả về tất cả work items của user. Bạn có thể filter ở phía frontend.

**Q: Có thể lấy work items của user khác không?**  
A: Không, API này chỉ trả về work items của user đang đăng nhập (từ JWT token).

**Q: Làm sao để biết work item đã hoàn thành chưa?**  
A: Kiểm tra trường `actualFinish` - nếu có giá trị thì đã hoàn thành, nếu null thì chưa hoàn thành.

**Q: Assignment có thể null không?**  
A: Có thể, nhưng trong thực tế rất hiếm vì work item luôn thuộc về một assignment. Nên kiểm tra null trước khi truy cập.

---

**Cập nhật lần cuối**: 2025-11-12

