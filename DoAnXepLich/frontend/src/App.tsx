import Dashboard from './pages/Dashboard/Dashboard'
import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom'
import { AuthProvider, useAuth } from './contexts/AuthContext'
import MainLayout from './layouts/MainLayout'
import DanhSachGiangVien from './pages/GiangVien/DanhSachGiangVien'
import DanhSachHocKy from './pages/HocKy/DanhSachHocKy'
import DanhSachHocPhan from './pages/HocPhan/DanhSachHocPhan'
import KetQuaXepLich from './pages/KetQua/KetQuaXepLich'
import DanhSachNamHoc from './pages/NamHoc/DanhSachNamHoc'
import DanhSachNguyenVong from './pages/NguyenVong/DanhSachNguyenVong'
import DanhSachRangBuoc from './pages/RangBuoc/DanhSachRangBuoc'
import DanhSachLopHocPhan from './pages/ThoiKhoaBieu/DanhSachLopHocPhan'
import ChayXepLich from './pages/XepLich/ChayXepLich'
import Login from './pages/Auth/Login'
import DanhSachNguoiDung from './pages/NguoiDung/DanhSachNguoiDung'
import DanhSachBoMon from './pages/BoMon/DanhSachBoMon'
import QuanLyLichSeminar from './pages/Seminar/QuanLyLichSeminar'

function ProtectedLayout() {
  const { user, dangTai } = useAuth()

  if (dangTai) {
    return <div className="p-4 text-center text-muted">Đang tải thông tin...</div>
  }

  if (!user) {
    return <Navigate to="/login" replace />
  }

  return <MainLayout />
}

interface RoleRouteProps {
  allowedRoles: string[]
  children: React.ReactNode
}

function RoleRoute({ allowedRoles, children }: RoleRouteProps) {
  const { user } = useAuth()
  if (!user || !allowedRoles.includes(user.vaiTro)) {
    return <Navigate to="/" replace />
  }
  return <>{children}</>
}

function TrangChuRedirect() {
  const { user } = useAuth()
  if (user?.vaiTro === 'Admin') {
    return <Navigate to="/tai-khoan" replace />
  }
  if (user?.vaiTro === 'TruongBoMon') {
    return <Navigate to="/dashboard" replace />
  }
  return <Navigate to="/thoi-khoa-bieu" replace />
}

const router = createBrowserRouter([
  {
    path: '/login',
    element: <Login />,
  },
  {
    path: '/',
    element: <ProtectedLayout />,
    children: [
      { index: true, element: <TrangChuRedirect /> },

      // Chức năng quản trị hệ thống (Chỉ dành cho Admin)
      {
        path: 'tai-khoan',
        element: (
          <RoleRoute allowedRoles={['Admin']}>
            <DanhSachNguoiDung />
          </RoleRoute>
        ),
      },
      {
        path: 'nam-hoc',
        element: (
          <RoleRoute allowedRoles={['Admin']}>
            <DanhSachNamHoc />
          </RoleRoute>
        ),
      },
      {
        path: 'hoc-ky',
        element: (
          <RoleRoute allowedRoles={['Admin']}>
            <DanhSachHocKy />
          </RoleRoute>
        ),
      },
      {
        path: 'bo-mon',
        element: (
          <RoleRoute allowedRoles={['Admin']}>
            <DanhSachBoMon />
          </RoleRoute>
        ),
      },

      // Thời khóa biểu & Import Excel (Admin import dữ liệu; Trưởng bộ môn & Giảng viên xem lịch)
      {
        path: 'thoi-khoa-bieu',
        element: (
          <RoleRoute allowedRoles={['Admin', 'TruongBoMon', 'GiangVien']}>
            <DanhSachLopHocPhan />
          </RoleRoute>
        ),
      },

      // Chức năng quản lý chuyên môn, thuật toán & xếp lịch (Chỉ dành cho Trưởng bộ môn)
      {
        path: 'dashboard',
        element: (
          <RoleRoute allowedRoles={['TruongBoMon']}>
            <Dashboard />
          </RoleRoute>
        ),
      },
      {
        path: 'giang-vien',
        element: (
          <RoleRoute allowedRoles={['TruongBoMon']}>
            <DanhSachGiangVien />
          </RoleRoute>
        ),
      },
      {
        path: 'hoc-phan',
        element: (
          <RoleRoute allowedRoles={['TruongBoMon']}>
            <DanhSachHocPhan />
          </RoleRoute>
        ),
      },
      {
        path: 'rang-buoc',
        element: (
          <RoleRoute allowedRoles={['TruongBoMon']}>
            <DanhSachRangBuoc />
          </RoleRoute>
        ),
      },
      {
        path: 'xep-lich',
        element: (
          <RoleRoute allowedRoles={['TruongBoMon']}>
            <ChayXepLich />
          </RoleRoute>
        ),
      },

      // Chức năng liên quan giảng viên (Trưởng bộ môn & Giảng viên)
      {
        path: 'nguyen-vong',
        element: (
          <RoleRoute allowedRoles={['TruongBoMon', 'GiangVien']}>
            <DanhSachNguyenVong />
          </RoleRoute>
        ),
      },
      {
        path: 'seminar',
        element: (
          <RoleRoute allowedRoles={['TruongBoMon', 'GiangVien']}>
            <QuanLyLichSeminar />
          </RoleRoute>
        ),
      },
      {
        path: 'ket-qua',
        element: (
          <RoleRoute allowedRoles={['TruongBoMon', 'GiangVien']}>
            <KetQuaXepLich />
          </RoleRoute>
        ),
      },
    ],
  },
])

function App() {
  return (
    <AuthProvider>
      <RouterProvider router={router} />
    </AuthProvider>
  )
}

export default App
