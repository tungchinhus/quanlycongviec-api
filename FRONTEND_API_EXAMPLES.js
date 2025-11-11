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
// 7. VÍ DỤ DỮ LIỆU TỪ FORM
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
// 8. SỬ DỤNG
// ============================================

// Ví dụ sử dụng:
/*
// 1. Login trước
await login('username', 'password');

// 2. Tạo assignment hoàn chỉnh
await createCompleteAssignment(exampleFormData);
*/

// Export cho module
if (typeof module !== 'undefined' && module.exports) {
  module.exports = {
    login,
    createAssignment,
    createWorkItem,
    createWorkChange,
    createCompleteAssignment
  };
}

