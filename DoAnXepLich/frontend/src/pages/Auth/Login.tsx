import logoXayDung from '../../assets/logo-xaydung.png'
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../contexts/AuthContext'

function Login() {
  const { dangNhap } = useAuth()
  const navigate = useNavigate()

  const [tenDangNhap, setTenDangNhap] = useState('')
  const [matKhau, setMatKhau] = useState('')
  const [loi, setLoi] = useState('')
  const [dangXuLy, setDangXuLy] = useState(false)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setLoi('')

    if (!tenDangNhap.trim() || !matKhau) {
      setLoi('Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.')
      return
    }

    try {
      setDangXuLy(true)
      await dangNhap({
        tenDangNhap: tenDangNhap.trim(),
        matKhau,
      })
      navigate('/')
    } catch (err: unknown) {
      console.error('Lỗi đăng nhập:', err)
      const errorObj = err as { response?: { data?: string | object; status?: number }; message?: string }
      if (errorObj?.response?.status === 401) {
        setLoi('Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.')
      } else if (errorObj?.message?.includes('Network Error')) {
        setLoi('Không thể kết nối đến máy chủ Backend (http://localhost:5001). Vui lòng kiểm tra Backend đã chạy chưa.')
      } else if (typeof errorObj?.response?.data === 'string') {
        setLoi(errorObj.response.data)
      } else {
        setLoi(errorObj?.message || 'Đăng nhập thất bại. Vui lòng thử lại.')
      }
    } finally {
      setDangXuLy(false)
    }
  }

  return (
    <div className="d-flex align-items-center justify-content-center min-vh-100 bg-light p-3">
      <div className="border bg-white p-4 shadow-sm" style={{ width: '100%', maxWidth: '380px' }}>
        <div className="text-center mb-4">
          <img src={logoXayDung} alt="Đại học Xây dựng" style={{ height: '56px', width: 'auto', marginBottom: '12px' }} />
          <h2 className="h4 mb-1">Xếp Lịch Giảng Dạy</h2>
          <p className="text-muted small mb-0">Hệ thống phân công thời khóa biểu nội bộ</p>
        </div>

        {loi && <div className="alert alert-danger py-2 small">{loi}</div>}

        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <label className="form-label small fw-semibold">Tên đăng nhập</label>
            <input
              type="text"
              className="form-control"
              value={tenDangNhap}
              onChange={(e) => setTenDangNhap(e.target.value)}
              placeholder="admin"
              autoFocus
            />
          </div>

          <div className="mb-3">
            <label className="form-label small fw-semibold">Mật khẩu</label>
            <input
              type="password"
              className="form-control"
              value={matKhau}
              onChange={(e) => setMatKhau(e.target.value)}
              placeholder="••••••"
            />
          </div>

          <button
            type="submit"
            className="btn btn-primary w-100 mb-3"
            disabled={dangXuLy}
          >
            {dangXuLy ? 'Đang đăng nhập...' : 'Đăng nhập'}
          </button>
        </form>

        <div className="border-top pt-3 text-muted small">
          <div>Tài khoản quản trị mặc định:</div>
          <div className="fw-semibold text-dark">admin / admin123</div>
        </div>
      </div>
    </div>
  )
}

export default Login
