import { NavLink } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'

interface SidebarProps {
  thuGon?: boolean
}

interface MenuItem {
  path: string
  label: string
  icon: string
}

const adminMenuItems: MenuItem[] = [
  { path: '/tai-khoan', label: 'Quản lý tài khoản', icon: 'user' },
  { path: '/nam-hoc', label: 'Quản lý năm học', icon: 'calendar' },
  { path: '/hoc-ky', label: 'Quản lý học kỳ', icon: 'layers' },
  { path: '/bo-mon', label: 'Quản lý bộ môn', icon: 'briefcase' },
  { path: '/giang-vien', label: 'Quản lý giảng viên', icon: 'users' },
  { path: '/hoc-phan', label: 'Quản lý học phần', icon: 'book' },
  { path: '/thoi-khoa-bieu', label: 'Thời khóa biểu', icon: 'table' },
  { path: '/rang-buoc', label: 'Cấu hình ràng buộc', icon: 'sliders' },
  { path: '/xep-lich', label: 'Xếp lịch tự động', icon: 'cpu' },
  { path: '/ket-qua', label: 'Kết quả xếp lịch', icon: 'check-square' },
]

const truongBoMonMenuItems: MenuItem[] = [
  { path: '/dashboard', label: 'Tổng quan & So sánh', icon: 'chart' },
  { path: '/giang-vien', label: 'Quản lý giảng viên', icon: 'users' },
  { path: '/hoc-phan', label: 'Quản lý học phần', icon: 'book' },
  { path: '/thoi-khoa-bieu', label: 'Thời khóa biểu', icon: 'table' },
  { path: '/nguyen-vong', label: 'Lịch bận & Nguyện vọng', icon: 'clock' },
  { path: '/rang-buoc', label: 'Cấu hình ràng buộc', icon: 'sliders' },
  { path: '/xep-lich', label: 'Xếp lịch tự động', icon: 'cpu' },
  { path: '/ket-qua', label: 'Kết quả xếp lịch', icon: 'check-square' },
]

const giangVienMenuItems: MenuItem[] = [
  { path: '/thoi-khoa-bieu', label: 'Thời khóa biểu', icon: 'table' },
  { path: '/nguyen-vong', label: 'Nguyện vọng', icon: 'clock' },
  { path: '/ket-qua', label: 'Kết quả xếp lịch', icon: 'check-square' },
]

function renderMenuIcon(icon: string) {
  switch (icon) {
    case 'chart':
      return (
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <line x1="18" y1="20" x2="18" y2="10" />
          <line x1="12" y1="20" x2="12" y2="4" />
          <line x1="6" y1="20" x2="6" y2="14" />
        </svg>
      )
    case 'user':
      return (
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
          <circle cx="12" cy="7" r="4" />
        </svg>
      )
    case 'users':
      return (
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
          <circle cx="9" cy="7" r="4" />
          <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
          <path d="M16 3.13a4 4 0 0 1 0 7.75" />
        </svg>
      )
    case 'calendar':
    case 'layers':
      return (
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
          <line x1="16" y1="2" x2="16" y2="6" />
          <line x1="8" y1="2" x2="8" y2="6" />
          <line x1="3" y1="10" x2="21" y2="10" />
        </svg>
      )
    case 'briefcase':
      return (
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <rect x="2" y="7" width="20" height="14" rx="2" ry="2" />
          <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16" />
        </svg>
      )
    case 'book':
      return (
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" />
          <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z" />
        </svg>
      )
    case 'table':
      return (
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <rect x="3" y="3" width="18" height="18" rx="2" />
          <line x1="3" y1="9" x2="21" y2="9" />
          <line x1="3" y1="15" x2="21" y2="15" />
          <line x1="9" y1="3" x2="9" y2="21" />
          <line x1="15" y1="3" x2="15" y2="21" />
        </svg>
      )
    case 'clock':
      return (
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="12" cy="12" r="10" />
          <polyline points="12 6 12 12 16 14" />
        </svg>
      )
    case 'sliders':
      return (
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <line x1="4" y1="21" x2="4" y2="14" />
          <line x1="4" y1="10" x2="4" y2="3" />
          <line x1="12" y1="21" x2="12" y2="12" />
          <line x1="12" y1="8" x2="12" y2="3" />
          <line x1="20" y1="21" x2="20" y2="16" />
          <line x1="20" y1="12" x2="20" y2="3" />
          <line x1="1" y1="14" x2="7" y2="14" />
          <line x1="9" y1="8" x2="15" y2="8" />
          <line x1="17" y1="16" x2="23" y2="16" />
        </svg>
      )
    case 'cpu':
      return (
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <rect x="4" y="4" width="16" height="16" rx="2" />
          <rect x="9" y="9" width="6" height="6" />
          <line x1="9" y1="1" x2="9" y2="4" />
          <line x1="15" y1="1" x2="15" y2="4" />
          <line x1="9" y1="20" x2="9" y2="23" />
          <line x1="15" y1="20" x2="15" y2="23" />
          <line x1="20" y1="9" x2="23" y2="9" />
          <line x1="20" y1="14" x2="23" y2="14" />
          <line x1="1" y1="9" x2="4" y2="9" />
          <line x1="1" y1="14" x2="4" y2="14" />
        </svg>
      )
    case 'check-square':
    default:
      return (
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <polyline points="9 11 12 14 22 4" />
          <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11" />
        </svg>
      )
  }
}

function Sidebar({ thuGon }: SidebarProps) {
  const { user } = useAuth()
  let items = giangVienMenuItems
  if (user?.vaiTro === 'Admin') {
    items = adminMenuItems
  } else if (user?.vaiTro === 'TruongBoMon') {
    items = truongBoMonMenuItems
  }

  return (
    <aside className={`sidebar ${thuGon ? 'thu-gon' : ''}`}>
      {/* Khung Logo ở đầu Sidebar theo phong cách mẫu ảnh */}
      <div className="sidebar-logo-box">
        <div className="sidebar-logo-badge" title="Thời khóa biểu Đại học">
          <span>ĐH</span>
        </div>
        {!thuGon && (
          <div className="ms-2 sidebar-header-title">
            <div className="fw-bold" style={{ fontSize: '13px', lineHeight: 1.2, color: '#0f172a' }}>
              XẾP LỊCH GIẢNG DẠY
            </div>
            <div className="text-muted" style={{ fontSize: '11px', letterSpacing: '0.2px' }}>
              Cổng thông tin đào tạo
            </div>
          </div>
        )}
      </div>

      {/* Danh sách các mục menu */}
      <nav className="sidebar-nav">
        {items.map((item) => (
          <NavLink
            key={item.path}
            to={item.path}
            title={thuGon ? item.label : undefined}
            className={({ isActive }) =>
              `sidebar-link ${isActive ? 'active' : ''}`
            }
          >
            <span className="sidebar-icon">{renderMenuIcon(item.icon)}</span>
            {!thuGon && <span className="sidebar-text">{item.label}</span>}
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}

export default Sidebar
