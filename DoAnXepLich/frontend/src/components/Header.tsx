import { useState, type FormEvent } from 'react'
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
  '/seminar': 'Lịch Seminar (Khóa cứng)',
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

      {/* Góc phải: Lời chào và Tài khoản cá nhân */}
      <div className="d-flex align-items-center gap-2">
        {user ? (
          <>
            <span style={{ fontSize: '13px', color: '#475569' }}>
              Xin chào, <strong style={{ color: '#0f172a' }}>{hienThiHoTen(user.hoTen)}</strong>
            </span>
            <span className="badge bg-light text-secondary border" style={{ fontSize: '11px' }}>
              {user.vaiTro === 'Admin' ? 'Quản trị' : user.vaiTro === 'TruongBoMon' ? 'Trưởng bộ môn' : user.vaiTro}
              {user.tenBoMon ? ` (${user.tenBoMon})` : ''}
            </span>

            <div className="btn-group ms-2">
              <button
                className="btn btn-outline-secondary btn-sm"
                onClick={() => {
                  setLoi('')
                  setThongBao('')
                  setHienModalDoiMk(true)
                }}
              >
                Đổi MK
              </button>
              <button
                className="btn btn-outline-danger btn-sm"
                onClick={handleDangXuat}
              >
                Đăng xuất
              </button>
            </div>
          </>
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
