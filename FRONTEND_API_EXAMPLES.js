/**
 * Ví dụ Frontend gọi API - QuanLyFiles Backend
 * Base URL: http://localhost:5000/api
 */

// ============================================
// 1. CẤU HÌNH
// ============================================

const API_BASE_URL = 'http://localhost:5000/api';
let authToken = null; // Lưu token sau khi login

// Hàm lấy headers với token
function getHeaders() {
  return {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${authToken}`
  };
}

// ============================================
// 2. AUTHENTICATION
// ============================================

// Đăng nhập
async function login(username, password) {
  try {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        userName: username,
        password: password
      })
    });

    if (!response.ok) {
      throw new Error('Login failed');
    }

    const data = await response.json();
    authToken = data.token; // Lưu token
    localStorage.setItem('token', data.token); // Lưu vào localStorage
    
    console.log('Login successful:', data.user);
    return data;
  } catch (error) {
    console.error('Login error:', error);
    throw error;
  }
}

// ============================================
// 3. TẠO ASSIGNMENT MỚI
// ============================================

/**
 * Tạo assignment mới
 * @param {Object} assignmentData - Dữ liệu assignment
 * @returns {Promise<Object>} Assignment đã tạo
 */
async function createAssignment(assignmentData) {
  try {
    const response = await fetch(`${API_BASE_URL}/assignments`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({
        tbkt_ID: assignmentData.requestDocument || assignmentData.tbkt_ID || '',
        machineName: assignmentData.machineName,
        standardRequirement: assignmentData.standardRequirement || null,
        additionalRequest: assignmentData.additionalRequest || null,
        deliveryDate: assignmentData.deliveryDate 
          ? new Date(assignmentData.deliveryDate).toISOString() 
          : null,
        designer: assignmentData.designer?.toString() || null,
        teamLeader: assignmentData.teamLeader?.toString() || null
      })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to create assignment');
    }

    const assignment = await response.json();
    console.log('Assignment created:', assignment);
    return assignment;
  } catch (error) {
    console.error('Create assignment error:', error);
    throw error;
  }
}

// ============================================
// 4. TẠO WORK ITEMS
// ============================================

/**
 * Tạo work item
 * @param {number} assignmentId - ID của assignment
 * @param {Object} workItemData - Dữ liệu work item
 * @returns {Promise<Object>} Work item đã tạo
 */
async function createWorkItem(assignmentId, workItemData) {
  try {
    const response = await fetch(`${API_BASE_URL}/assignments/${assignmentId}/work-items`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({
        assignmentID: assignmentId,
        workType: workItemData.workType,
        personName: workItemData.personName?.toString() || null,
        startDate: workItemData.startDate 
          ? new Date(workItemData.startDate).toISOString() 
          : null,
        expectedFinish: workItemData.expectedFinish 
          ? new Date(workItemData.expectedFinish).toISOString() 
          : null,
        actualFinish: workItemData.actualFinish 
          ? new Date(workItemData.actualFinish).toISOString() 
          : null,
        personConfirmation: workItemData.personConfirmation || null,
        notes: workItemData.notes || null
      })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to create work item');
    }

    const workItem = await response.json();
    console.log('Work item created:', workItem);
    return workItem;
  } catch (error) {
    console.error('Create work item error:', error);
    throw error;
  }
}

// ============================================
// 5. TẠO WORK CHANGE
// ============================================

/**
 * Tạo work change
 * @param {number} assignmentId - ID của assignment
 * @param {string} description - Mô tả thay đổi
 * @returns {Promise<Object>} Work change đã tạo
 */
async function createWorkChange(assignmentId, description) {
  try {
    const response = await fetch(`${API_BASE_URL}/assignments/${assignmentId}/work-changes`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({
        assignmentID: assignmentId,
        changeType: 'Change Request',
        description: description
      })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to create work change');
    }

    const workChange = await response.json();
    console.log('Work change created:', workChange);
    return workChange;
  } catch (error) {
    console.error('Create work change error:', error);
    throw error;
  }
}

// ============================================
// 6. VÍ DỤ SỬ DỤNG - TẠO ASSIGNMENT HOÀN CHỈNH
// ============================================

/**
 * Tạo assignment hoàn chỉnh với tất cả work items
 * @param {Object} formData - Dữ liệu từ form
 */
async function createCompleteAssignment(formData) {
  try {
    // 1. Đảm bảo đã login
    if (!authToken) {
      authToken = localStorage.getItem('token');
      if (!authToken) {
        throw new Error('Please login first');
      }
    }

    // 2. Tạo assignment chính
    const assignment = await createAssignment({
      requestDocument: formData.requestDocument,
      machineName: formData.machineName,
      standardRequirement: formData.standardRequirement,
      additionalRequest: formData.additionalRequest,
      deliveryDate: formData.deliveryDate,
      designer: formData.designer,
      teamLeader: formData.teamLeader
    });

    const assignmentId = assignment.assignmentID;

    // 3. Tạo các work items
    const workItemPromises = [];

    // Core Design
    if (formData.coreDesignUser) {
      workItemPromises.push(
        createWorkItem(assignmentId, {
          workType: 'Core Design',
          personName: formData.coreDesignUser
        })
      );
    }

    // Core Review
    if (formData.coreReviewUser) {
      workItemPromises.push(
        createWorkItem(assignmentId, {
          workType: 'Core Review',
          personName: formData.coreReviewUser
        })
      );
    }

    // Casing Design
    if (formData.casingDesignUser) {
      workItemPromises.push(
        createWorkItem(assignmentId, {
          workType: 'Casing Design',
          personName: formData.casingDesignUser
        })
      );
    }

    // Casing Review
    if (formData.casingReviewUser) {
      workItemPromises.push(
        createWorkItem(assignmentId, {
          workType: 'Casing Review',
          personName: formData.casingReviewUser
        })
      );
    }

    // Material Leveling
    if (formData.materialLevelingUser) {
      workItemPromises.push(
        createWorkItem(assignmentId, {
          workType: 'Material Leveling',
          personName: formData.materialLevelingUser
        })
      );
    }

    // 4. Tạo work change nếu có
    if (formData.workChanges) {
      await createWorkChange(assignmentId, formData.workChanges);
    }

    // 5. Đợi tất cả work items được tạo
    await Promise.all(workItemPromises);

    console.log('Complete assignment created successfully!');
    return assignment;

  } catch (error) {
    console.error('Error creating complete assignment:', error);
    throw error;
  }
}

// ============================================
// 7. UPLOAD FILE
// ============================================

/**
 * Upload file cho assignment
 * @param {File} file - File object từ input
 * @param {number} assignmentId - ID của assignment
 * @param {string} description - Mô tả file (optional)
 * @returns {Promise<Object>} File đã upload
 */
async function uploadFile(file, assignmentId, description = null) {
  try {
    // Validate file size (50MB)
    if (file.size > 50 * 1024 * 1024) {
      throw new Error('File size exceeds 50MB');
    }

    const formData = new FormData();
    formData.append('file', file);
    formData.append('assignmentId', assignmentId.toString());
    if (description) {
      formData.append('description', description);
    }

    const response = await fetch(`${API_BASE_URL}/files/upload`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${authToken}`
        // KHÔNG set Content-Type, browser sẽ tự động set multipart/form-data
      },
      body: formData
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Upload failed');
    }

    const uploadedFile = await response.json();
    console.log('File uploaded:', uploadedFile);
    return uploadedFile;
  } catch (error) {
    console.error('Upload file error:', error);
    throw error;
  }
}

/**
 * Lấy tất cả files của một assignment
 * @param {number} assignmentId - ID của assignment
 * @returns {Promise<Array>} Danh sách files
 */
async function getFilesByAssignment(assignmentId) {
  try {
    const response = await fetch(`${API_BASE_URL}/files/byAssignment/${assignmentId}`, {
      headers: getHeaders()
    });

    if (!response.ok) {
      throw new Error('Failed to get files');
    }

    const files = await response.json();
    console.log('Files:', files);
    return files;
  } catch (error) {
    console.error('Get files error:', error);
    throw error;
  }
}

/**
 * Xóa file
 * @param {number} fileId - ID của file
 * @returns {Promise<void>}
 */
async function deleteFile(fileId) {
  try {
    const response = await fetch(`${API_BASE_URL}/files/${fileId}`, {
      method: 'DELETE',
      headers: getHeaders()
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Delete failed');
    }

    console.log('File deleted successfully');
  } catch (error) {
    console.error('Delete file error:', error);
    throw error;
  }
}

// ============================================
// 8. VÍ DỤ SỬ DỤNG - UPLOAD FILE
// ============================================

/**
 * Ví dụ upload file cho assignment
 */
async function exampleUploadFile() {
  try {
    // 1. Đảm bảo đã login
    if (!authToken) {
      authToken = localStorage.getItem('token');
      if (!authToken) {
        throw new Error('Please login first');
      }
    }

    // 2. Lấy file từ input
    const fileInput = document.getElementById('fileInput');
    if (!fileInput || !fileInput.files[0]) {
      throw new Error('Please select a file');
    }

    const file = fileInput.files[0];
    const assignmentId = 123; // Lấy từ form hoặc state
    const description = 'File mô tả kỹ thuật';

    // 3. Upload file
    const uploadedFile = await uploadFile(file, assignmentId, description);
    console.log('Uploaded file:', uploadedFile);

    // 4. Lấy danh sách files của assignment
    const files = await getFilesByAssignment(assignmentId);
    console.log('All files:', files);

    // 5. Xóa file (nếu cần)
    // await deleteFile(uploadedFile.id);
  } catch (error) {
    console.error('Example upload error:', error);
  }
}

// ============================================
// 9. VÍ DỤ SỬ DỤNG - TẠO ASSIGNMENT VÀ UPLOAD FILE
// ============================================

/**
 * Tạo assignment hoàn chỉnh và upload file
 */
async function createAssignmentWithFiles(formData, files) {
  try {
    // 1. Tạo assignment
    const assignment = await createCompleteAssignment(formData);
    const assignmentId = assignment.assignmentID;

    // 2. Upload các files
    const uploadPromises = files.map(file => 
      uploadFile(file, assignmentId, `File cho ${assignment.machineName}`)
    );

    const uploadedFiles = await Promise.all(uploadPromises);
    console.log('All files uploaded:', uploadedFiles);

    return {
      assignment,
      files: uploadedFiles
    };
  } catch (error) {
    console.error('Create assignment with files error:', error);
    throw error;
  }
}

// ============================================
// 10. HTML EXAMPLE
// ============================================

/*
<!-- HTML Example -->
<input type="file" id="fileInput" accept=".pdf,.doc,.docx,.xls,.xlsx">
<button onclick="exampleUploadFile()">Upload File</button>

<script>
  // Sử dụng các function đã định nghĩa ở trên
</script>
*/

// ============================================
// EXPORT (nếu sử dụng module)
// ============================================

// Export cho module
if (typeof module !== 'undefined' && module.exports) {
  module.exports = {
    login,
    createAssignment,
    createWorkItem,
    createWorkChange,
    createCompleteAssignment,
    uploadFile,
    getFilesByAssignment,
    deleteFile,
    createAssignmentWithFiles
  };
}

// ============================================
// 11. VÍ DỤ DỮ LIỆU TỪ FORM
// ============================================

// Dữ liệu mẫu từ form "Tạo Giao Việc Mới"
const exampleFormData = {
  machineName: "MBA 3 pha 250kVA, 35±2x2.5%/0.4kV Dyn11 (unicore 5 tru)",
  additionalRequest: "TC: 96/QĐ-HDTV",
  casingDesignUser: "12",
  casingReviewUser: "6",
  coreDesignUser: "18",
  coreReviewUser: "15",
  deliveryDate: "2025-11-29T17:00:00.000Z",
  designer: "2",
  materialLevelingUser: "20",
  requestDocument: "Hàng nền",
  standardRequirement: "Io ≤ 2.0% (+30%); Po ≤ 340 W; Pk ≤ 2600 W; Uk ≥ 4.0%\nDầu cách điện POWEROIL TO 1020 60 HX",
  teamLeader: "4",
  workChanges: "ghfhggh"
};

// ============================================
// 8. LẤY DANH SÁCH WORK ITEMS CỦA USER ĐĂNG NHẬP
// ============================================

/**
 * Lấy danh sách work items (công việc) của user đang đăng nhập
 * @returns {Promise<Array>} Danh sách work items với thông tin assignment
 */
async function getMyWorkItems() {
  try {
    // Đảm bảo đã login
    if (!authToken) {
      authToken = localStorage.getItem('token');
      if (!authToken) {
        throw new Error('Please login first');
      }
    }

    const response = await fetch(`${API_BASE_URL}/assignments/my-work-items`, {
      method: 'GET',
      headers: getHeaders()
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

// ============================================
// 9. SỬ DỤNG
// ============================================

// Ví dụ sử dụng:
/*
// 1. Login trước
await login('username', 'password');

// 2. Tạo assignment hoàn chỉnh
await createCompleteAssignment(exampleFormData);

// 3. Lấy danh sách work items của user đăng nhập
const myWorkItems = await getMyWorkItems();
console.log('Công việc của tôi:', myWorkItems);
*/

// Export cho module
if (typeof module !== 'undefined' && module.exports) {
  module.exports = {
    login,
    createAssignment,
    createWorkItem,
    createWorkChange,
    createCompleteAssignment,
    getMyWorkItems
  };
}

