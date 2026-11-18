import { useEffect, useRef, useState, type FormEvent } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'

interface HeaderProps {
  sidebarThuGon?: boolean
  onToggleSidebar?: () => void
}

// Ánh xạ đường dẫn tới tiêu đề trang hiển thị trên Header chuẩn như ảnh mẫu
const pageTitles: Record<string, string> = {
  '/dashboard': 'Tổng quan & So sánh thuật toán',
  '/thoi-khoa-bieu': 'Thời khóa biểu & Lớp học phần',
  '/xep-lich': 'Xếp lịch tự động',
  '/ket-qua': 'Kết quả xếp lịch & Phân công',
  '/giang-vien': 'Quản lý giảng viên',
  '/hoc-phan': 'Quản lý học phần',
  '/rang-buoc': 'Cấu hình ràng buộc & Định mức',
  '/nguyen-vong': 'Lịch bận & Nguyện vọng',
  '/seminar': 'Lịch Seminar',
  '/tai-khoan': 'Quản lý tài khoản',
  '/nam-hoc': 'Quản lý năm học',
  '/hoc-ky': 'Quản lý học kỳ',
  '/bo-mon': 'Quản lý bộ môn',
}

function Header({ onToggleSidebar }: HeaderProps) {
  const { user, dangXuat, doiMatKhau } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const [hienModalDoiMk, setHienModalDoiMk] = useState(false)
  const [matKhauCu, setMatKhauCu] = useState('')
  const [matKhauMoi, setMatKhauMoi] = useState('')
  const [loi, setLoi] = useState('')
  const [thongBao, setThongBao] = useState('')
  const [moMenu, setMoMenu] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!moMenu) return

    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setMoMenu(false)
      }
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setMoMenu(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [moMenu])

  const handleDangXuat = () => {
    dangXuat()
    navigate('/login')
  }

  const handleDoiMatKhau = async (e: FormEvent) => {
    e.preventDefault()
    setLoi('')
    setThongBao('')

    if (!matKhauCu || !matKhauMoi) {
      setLoi('Vui lòng nhập đầy đủ mật khẩu cũ và mới.')
      return
    }

    try {
      await doiMatKhau(matKhauCu, matKhauMoi)
      setThongBao('Đổi mật khẩu thành công!')
      setMatKhauCu('')
      setMatKhauMoi('')
      setTimeout(() => {
        setHienModalDoiMk(false)
        setThongBao('')
      }, 1200)
    } catch {
      setLoi('Mật khẩu cũ không đúng.')
    }
  }

  const hienThiHoTen = (ten?: string) => {
    if (!ten) return ''
    if (ten === 'Quan tri vien he thong') return 'Quản trị viên hệ thống'
    return ten
  }

  const tieuDeTrangHienTai = pageTitles[location.pathname] || 'Phân công giảng dạy'

  const tenHienThi = user ? hienThiHoTen(user.hoTen) : ''
  const cacTuTen = tenHienThi.trim() ? tenHienThi.trim().split(/\s+/) : []
  const chuCaiDau = cacTuTen.length > 0 ? cacTuTen[cacTuTen.length - 1].charAt(0).toUpperCase() : '?'
  const vaiTroHienThi = user
    ? `${
        user.vaiTro === 'Admin'
          ? 'Quản trị'
          : user.vaiTro === 'TruongBoMon'
            ? 'Trưởng bộ môn'
            : user.vaiTro
      }${user.tenBoMon ? ` (${user.tenBoMon})` : ''}`
    : ''

  return (
    <header className="header-bar">
      {/* Góc trái: Nút thu gọn Sidebar và Tiêu đề chức năng hiện tại */}
      <div className="d-flex align-items-center gap-3">
        <button
          type="button"
          className="btn btn-outline-secondary btn-sm px-2"
          onClick={onToggleSidebar}
          title="Thu gọn / Mở rộng menu"
          style={{ width: '32px', height: '32px', border: '1px solid #cbd5e1' }}
        >
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <line x1="3" y1="12" x2="21" y2="12" />
            <line x1="3" y1="6" x2="21" y2="6" />
            <line x1="3" y1="18" x2="21" y2="18" />
          </svg>
        </button>

        <span className="fw-semibold" style={{ fontSize: '15px', color: '#0f172a' }}>
          {tieuDeTrangHienTai}
        </span>
      </div>

      {/* Góc phải: Menu tài khoản cá nhân */}
      <div className="d-flex align-items-center gap-2">
        {user ? (
          <div ref={menuRef} style={{ position: 'relative' }}>
            <button
              type="button"
              className="header-account-trigger"
              aria-haspopup="menu"
              aria-expanded={moMenu}
              onClick={() => setMoMenu((prev) => !prev)}
            >
              <span
                style={{
                  width: '26px',
                  height: '26px',
                  borderRadius: '50%',
                  backgroundColor: '#111827',
                  color: '#ffffff',
                  fontWeight: 700,
                  fontSize: '12px',
                  display: 'inline-flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                }}
              >
                {chuCaiDau}
              </span>

              <span style={{ fontSize: '14px', color: '#334155', whiteSpace: 'nowrap' }}>
                {tenHienThi}
              </span>

              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.8"
                strokeLinecap="round"
                strokeLinejoin="round"
                style={{
                  color: '#64748b',
                  transform: moMenu ? 'rotate(180deg)' : 'rotate(0deg)',
                  transition: 'transform 0.2s ease',
                  flexShrink: 0,
                }}
              >
                <polyline points="6 9 12 15 18 9" />
              </svg>
            </button>

            {moMenu && (
              <div
                role="menu"
                style={{
                  position: 'absolute',
                  right: 0,
                  top: 'calc(100% + 8px)',
                  width: '260px',
                  backgroundColor: '#ffffff',
                  border: '1px solid #e2e8f0',
                  borderRadius: '12px',
                  boxShadow: '0 10px 25px -5px rgba(15, 23, 42, 0.08), 0 4px 10px -2px rgba(15, 23, 42, 0.04)',
                  padding: '6px',
                  zIndex: 1050,
                }}
              >
                <div style={{ padding: '8px 12px 10px' }}>
                  <div style={{ fontSize: '15px', fontWeight: 500, color: '#0f172a', lineHeight: 1.35 }}>
                    {tenHienThi}
                  </div>
                  <div style={{ fontSize: '13px', color: '#94a3b8', marginTop: '2px', lineHeight: 1.35 }}>
                    {vaiTroHienThi}
                  </div>
                </div>

                <div style={{ height: '1px', backgroundColor: '#f1f5f9', margin: '2px 0 6px' }} />

                <button
                  type="button"
                  role="menuitem"
                  className="header-account-item"
                  onClick={() => {
                    setMoMenu(false)
                    setLoi('')
                    setThongBao('')
                    setHienModalDoiMk(true)
                  }}
                >
                  <svg
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="1.8"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    style={{ flexShrink: 0 }}
                  >
                    <path d="M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.778 7.778 5.5 5.5 0 0 1 7.777-7.777zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4" />
                  </svg>
                  <span>Đổi mật khẩu</span>
                </button>

                <button
                  type="button"
                  role="menuitem"
                  className="header-account-item header-account-item-danger"
                  onClick={() => {
                    setMoMenu(false)
                    handleDangXuat()
                  }}
                >
                  <svg
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="1.8"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    style={{ flexShrink: 0 }}
                  >
                    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                    <polyline points="16 17 21 12 16 7" />
                    <line x1="21" y1="12" x2="9" y2="12" />
                  </svg>
                  <span>Đăng xuất</span>
                </button>
              </div>
            )}
          </div>
        ) : (
          <button
            className="btn btn-primary btn-sm"
            onClick={() => navigate('/login')}
          >
            Đăng nhập
          </button>
        )}
      </div>

      {/* Modal Đổi Mật Khẩu */}
      {hienModalDoiMk && (
        <div
          className="modal show d-block"
          style={{ backgroundColor: 'rgba(0,0,0,0.45)' }}
        >
          <div className="modal-dialog modal-sm modal-dialog-centered">
            <div className="modal-content" style={{ borderRadius: '4px', border: '1px solid #cbd5e1' }}>
              <form onSubmit={handleDoiMatKhau}>
                <div className="modal-header py-2" style={{ borderBottom: '1px solid #e2e8f0' }}>
                  <h5 className="modal-title h6 mb-0">Đổi mật khẩu</h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={() => setHienModalDoiMk(false)}
                  />
                </div>
                <div className="modal-body">
                  {loi && <div className="alert alert-danger py-1 small">{loi}</div>}
                  {thongBao && <div className="alert alert-success py-1 small">{thongBao}</div>}

                  <div className="mb-2">
                    <label className="form-label small fw-semibold">Mật khẩu hiện tại</label>
                    <input
                      type="password"
                      className="form-control form-control-sm"
                      value={matKhauCu}
                      onChange={(e) => setMatKhauCu(e.target.value)}
                      required
                    />
                  </div>
                  <div className="mb-2">
                    <label className="form-label small fw-semibold">Mật khẩu mới</label>
                    <input
                      type="password"
                      className="form-control form-control-sm"
                      value={matKhauMoi}
                      onChange={(e) => setMatKhauMoi(e.target.value)}
                      required
                    />
                  </div>
                </div>
                <div className="modal-footer py-2" style={{ borderTop: '1px solid #e2e8f0' }}>
                  <button
                    type="button"
                    className="btn btn-secondary btn-sm"
                    onClick={() => setHienModalDoiMk(false)}
                  >
                    Hủy
                  </button>
                  <button type="submit" className="btn btn-primary btn-sm">
                    Lưu mật khẩu
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </header>
  )
}

export default Header
