import { useEffect, useState, type FormEvent } from 'react'
import { giangVienApi } from '../../api/giangVienApi'
import { boMonApi } from '../../api/boMonApi'
import { nguoiDungApi } from '../../api/nguoiDungApi'
import Loading from '../../components/Loading'
import { useAuth } from '../../contexts/AuthContext'
import type { GiangVien } from '../../types/giangVien'
import type { BoMon } from '../../types/boMon'
import type { NguoiDung } from '../../types/nguoiDung'

function DanhSachNguoiDung() {
  const { user: currentUser } = useAuth()

  const [danhSach, setDanhSach] = useState<NguoiDung[]>([])
  const [danhSachGv, setDanhSachGv] = useState<GiangVien[]>([])
  const [danhSachBoMon, setDanhSachBoMon] = useState<BoMon[]>([])
  const [dangTai, setDangTai] = useState(true)
  const [loi, setLoi] = useState('')
  const [thongBao, setThongBao] = useState('')

  // State Modal Thêm / Sửa
  const [hienModal, setHienModal] = useState(false)
  const [dangSuaId, setDangSuaId] = useState<number | null>(null)
  const [formTenDangNhap, setFormTenDangNhap] = useState('')
  const [formMatKhau, setFormMatKhau] = useState('')
  const [formHoTen, setFormHoTen] = useState('')
  const [formEmail, setFormEmail] = useState('')
  const [formVaiTro, setFormVaiTro] = useState('GiangVien')
  const [formMaGiangVien, setFormMaGiangVien] = useState<number | undefined>(undefined)
  const [formMaBoMon, setFormMaBoMon] = useState<number | undefined>(undefined)
  const [formTrangThai, setFormTrangThai] = useState('KichHoat')

  // State Modal Reset Mật khẩu
  const [hienModalReset, setHienModalReset] = useState(false)
  const [userResetId, setUserResetId] = useState<number | null>(null)
  const [matKhauReset, setMatKhauReset] = useState('')

  const taiDuLieu = async () => {
    try {
      setDangTai(true)
      const [resUsers, resGv, resBm] = await Promise.all([
        nguoiDungApi.getAll(),
        giangVienApi.getAll(),
        boMonApi.getAll(),
      ])
      setDanhSach(resUsers.data)
      setDanhSachGv(resGv.data)
      setDanhSachBoMon(resBm.data)
    } catch {
      setLoi('Không tải được danh sách người dùng.')
    } finally {
      setDangTai(false)
    }
  }

  useEffect(() => {
    void taiDuLieu()
  }, [])

  const moModalThem = () => {
    setDangSuaId(null)
    setFormTenDangNhap('')
    setFormMatKhau('')
    setFormHoTen('')
    setFormEmail('')
    setFormVaiTro('GiangVien')
    setFormMaGiangVien(undefined)
    setFormMaBoMon(undefined)
    setFormTrangThai('KichHoat')
    setHienModal(true)
  }

  const moModalSua = (u: NguoiDung) => {
    setDangSuaId(u.maNguoiDung)
    setFormTenDangNhap(u.tenDangNhap)
    setFormMatKhau('')
    setFormHoTen(u.hoTen)
    setFormEmail(u.email || '')
    setFormVaiTro(u.vaiTro)
    setFormMaGiangVien(u.maGiangVien)
    setFormMaBoMon(u.maBoMon)
    setFormTrangThai(u.trangThai)
    setHienModal(true)
  }

  const handleChonGiangVien = (gvIdStr: string) => {
    if (!gvIdStr) {
      setFormMaGiangVien(undefined)
      return
    }
    const gvId = Number(gvIdStr)
    setFormMaGiangVien(gvId)
    const gv = danhSachGv.find((x) => x.maGiangVien === gvId)
    if (gv) {
      if (gv.maBoMon) setFormMaBoMon(gv.maBoMon)
      if (!dangSuaId) {
        if (!formHoTen) setFormHoTen(gv.hoTen)
        if (!formEmail && gv.email) setFormEmail(gv.email)
      }
    }
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setLoi('')
    setThongBao('')

    if (!formHoTen.trim()) {
      setLoi('Vui lòng nhập họ tên người dùng.')
      return
    }

    try {
      if (dangSuaId) {
        await nguoiDungApi.update(dangSuaId, {
          hoTen: formHoTen.trim(),
          email: formEmail.trim() || undefined,
          vaiTro: formVaiTro,
          maGiangVien: formMaGiangVien,
          maBoMon: formMaBoMon,
          trangThai: formTrangThai,
        })
        setThongBao('Cập nhật người dùng thành công.')
      } else {
        if (!formTenDangNhap.trim() || !formMatKhau) {
          setLoi('Vui lòng nhập tên đăng nhập và mật khẩu.')
          return
        }
        await nguoiDungApi.create({
          tenDangNhap: formTenDangNhap.trim(),
          matKhau: formMatKhau,
          hoTen: formHoTen.trim(),
          email: formEmail.trim() || undefined,
          vaiTro: formVaiTro,
          maGiangVien: formMaGiangVien,
          maBoMon: formMaBoMon,
        })
        setThongBao('Tạo mới người dùng thành công.')
      }

      setHienModal(false)
      await taiDuLieu()
    } catch {
      setLoi('Thao tác thất bại. Tên đăng nhập có thể đã tồn tại.')
    }
  }

  const handleToggleKhoa = async (u: NguoiDung) => {
    if (u.maNguoiDung === currentUser?.maNguoiDung) {
      alert('Bạn không thể khóa chính tài khoản của mình.')
      return
    }
    const trangThaiMoi = u.trangThai === 'KichHoat' ? 'Khoa' : 'KichHoat'
    try {
      await nguoiDungApi.update(u.maNguoiDung, {
        hoTen: u.hoTen,
        email: u.email,
        vaiTro: u.vaiTro,
        maGiangVien: u.maGiangVien,
        maBoMon: u.maBoMon,
        trangThai: trangThaiMoi,
      })
      await taiDuLieu()
    } catch {
      alert('Không cập nhật được trạng thái tài khoản.')
    }
  }

  const handleXoa = async (u: NguoiDung) => {
    if (u.maNguoiDung === currentUser?.maNguoiDung) {
      alert('Bạn không thể xóa chính tài khoản của mình.')
      return
    }
    if (u.tenDangNhap === 'admin') {
      alert('Không thể xóa tài khoản admin gốc.')
      return
    }
    if (!window.confirm(`Bạn có chắc muốn xóa tài khoản "${u.tenDangNhap}"?`)) {
      return
    }

    try {
      await nguoiDungApi.delete(u.maNguoiDung)
      setThongBao('Xóa tài khoản thành công.')
      await taiDuLieu()
    } catch {
      setLoi('Không xóa được tài khoản.')
    }
  }

  const moModalResetMatKhau = (u: NguoiDung) => {
    setUserResetId(u.maNguoiDung)
    setMatKhauReset('')
    setHienModalReset(true)
  }

  const handleResetMatKhau = async (e: FormEvent) => {
    e.preventDefault()
    if (!userResetId || !matKhauReset) {
      alert('Vui lòng nhập mật khẩu mới.')
      return
    }

    try {
      await nguoiDungApi.resetMatKhau(userResetId, matKhauReset)
      alert('Đặt lại mật khẩu thành công.')
      setHienModalReset(false)
    } catch {
      alert('Đặt lại mật khẩu thất bại.')
    }
  }

  const getVaiTroBadge = (vaiTro: string) => {
    switch (vaiTro) {
      case 'Admin':
        return <span className="badge bg-danger">Quản trị viên</span>
      case 'TruongBoMon':
        return <span className="badge bg-warning text-dark">Trưởng bộ môn</span>
      default:
        return <span className="badge bg-secondary">Giảng viên</span>
    }
  }

  return (
    <section>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 className="h4 mb-0">Quản lý người dùng & phân quyền</h2>
          <small className="text-muted">
            Quản trị viên cấp tài khoản cho Trưởng bộ môn và Giảng viên
          </small>
        </div>
        <button className="btn btn-primary btn-sm" onClick={moModalThem}>
          + Thêm tài khoản mới
        </button>
      </div>

      {thongBao && <div className="alert alert-success py-2 small">{thongBao}</div>}
      {loi && <div className="alert alert-danger py-2 small">{loi}</div>}

      {dangTai && <Loading />}

      {!dangTai && (
        <div className="table-responsive border bg-white">
          <table className="table table-hover table-bordered mb-0 align-middle">
            <thead className="table-light">
              <tr>
                <th style={{ width: '50px' }}>STT</th>
                <th>Tên đăng nhập</th>
                <th>Họ tên</th>
                <th>Email</th>
                <th>Vai trò</th>
                <th>Bộ môn</th>
                <th>Giảng viên liên kết</th>
                <th>Trạng thái</th>
                <th style={{ width: '220px' }}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {danhSach.length === 0 ? (
                <tr>
                  <td colSpan={9} className="text-center text-muted py-3">
                    Chưa có tài khoản nào.
                  </td>
                </tr>
              ) : (
                danhSach.map((u, idx) => (
                  <tr key={u.maNguoiDung}>
                    <td className="text-center">{idx + 1}</td>
                    <td className="fw-semibold">{u.tenDangNhap}</td>
                    <td>{u.hoTen}</td>
                    <td>{u.email || '-'}</td>
                    <td>{getVaiTroBadge(u.vaiTro)}</td>
                    <td>
                      {u.tenBoMon ? (
                        <span className="badge bg-light text-primary border">{u.tenBoMon}</span>
                      ) : (
                        <span className="text-muted small">-</span>
                      )}
                    </td>
                    <td>{u.tenGiangVien || <span className="text-muted small">Không liên kết</span>}</td>
                    <td>
                      {u.trangThai === 'KichHoat' ? (
                        <span className="badge bg-success">Kích hoạt</span>
                      ) : (
                        <span className="badge bg-danger">Đã khóa</span>
                      )}
                    </td>
                    <td>
                      <div className="btn-group btn-group-sm">
                        <button
                          className="btn btn-outline-secondary"
                          onClick={() => moModalSua(u)}
                          title="Sửa thông tin"
                        >
                          Sửa
                        </button>
                        <button
                          className="btn btn-outline-warning"
                          onClick={() => moModalResetMatKhau(u)}
                          title="Đặt lại mật khẩu"
                        >
                          Mật khẩu
                        </button>
                        <button
                          className={`btn ${
                            u.trangThai === 'KichHoat'
                              ? 'btn-outline-danger'
                              : 'btn-outline-success'
                          }`}
                          onClick={() => handleToggleKhoa(u)}
                          title={u.trangThai === 'KichHoat' ? 'Khóa tài khoản' : 'Mở khóa'}
                        >
                          {u.trangThai === 'KichHoat' ? 'Khóa' : 'Mở'}
                        </button>
                        <button
                          className="btn btn-outline-danger"
                          onClick={() => handleXoa(u)}
                          title="Xóa tài khoản"
                        >
                          Xóa
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Modal Thêm / Sửa tài khoản */}
      {hienModal && (
        <div
          className="modal show d-block"
          style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}
        >
          <div className="modal-dialog">
            <div className="modal-content">
              <form onSubmit={handleSubmit}>
                <div className="modal-header py-2">
                  <h5 className="modal-title h6">
                    {dangSuaId ? 'Sửa tài khoản người dùng' : 'Thêm tài khoản mới'}
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={() => setHienModal(false)}
                  />
                </div>
                <div className="modal-body">
                  {!dangSuaId && (
                    <>
                      <div className="mb-2">
                        <label className="form-label small fw-semibold">
                          Tên đăng nhập <span className="text-danger">*</span>
                        </label>
                        <input
                          type="text"
                          className="form-control form-control-sm"
                          value={formTenDangNhap}
                          onChange={(e) => setFormTenDangNhap(e.target.value)}
                          placeholder="Ví dụ: gv_nguyenvana"
                          required
                        />
                      </div>
                      <div className="mb-2">
                        <label className="form-label small fw-semibold">
                          Mật khẩu ban đầu <span className="text-danger">*</span>
                        </label>
                        <input
                          type="password"
                          className="form-control form-control-sm"
                          value={formMatKhau}
                          onChange={(e) => setFormMatKhau(e.target.value)}
                          placeholder="Nhập mật khẩu"
                          required
                        />
                      </div>
                    </>
                  )}

                  <div className="mb-2">
                    <label className="form-label small fw-semibold">
                      Họ tên người dùng <span className="text-danger">*</span>
                    </label>
                    <input
                      type="text"
                      className="form-control form-control-sm"
                      value={formHoTen}
                      onChange={(e) => setFormHoTen(e.target.value)}
                      placeholder="Ví dụ: Nguyễn Văn A"
                      required
                    />
                  </div>

                  <div className="mb-2">
                    <label className="form-label small fw-semibold">Email</label>
                    <input
                      type="email"
                      className="form-control form-control-sm"
                      value={formEmail}
                      onChange={(e) => setFormEmail(e.target.value)}
                      placeholder="a@truong.edu.vn"
                    />
                  </div>

                  <div className="mb-2">
                    <label className="form-label small fw-semibold">Vai trò (Phân quyền)</label>
                    <select
                      className="form-select form-select-sm"
                      value={formVaiTro}
                      onChange={(e) => setFormVaiTro(e.target.value)}
                    >
                      <option value="GiangVien">Giảng viên</option>
                      <option value="TruongBoMon">Trưởng bộ môn</option>
                      <option value="Admin">Quản trị viên (Admin)</option>
                    </select>
                  </div>

                  <div className="mb-2">
                    <label className="form-label small fw-semibold">
                      Liên kết hồ sơ Giảng viên (nếu có)
                    </label>
                    <select
                      className="form-select form-select-sm"
                      value={formMaGiangVien || ''}
                      onChange={(e) => handleChonGiangVien(e.target.value)}
                    >
                      <option value="">-- Không liên kết (Tài khoản độc lập) --</option>
                      {danhSachGv.map((gv) => (
                        <option key={gv.maGiangVien} value={gv.maGiangVien}>
                          {gv.hoTen} {gv.email ? `(${gv.email})` : ''}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="mb-2">
                    <label className="form-label small fw-semibold">
                      Bộ môn công tác / phụ trách
                    </label>
                    <select
                      className="form-select form-select-sm"
                      value={formMaBoMon || ''}
                      onChange={(e) => setFormMaBoMon(e.target.value ? Number(e.target.value) : undefined)}
                    >
                      <option value="">-- Chọn bộ môn --</option>
                      {danhSachBoMon.map((bm) => (
                        <option key={bm.maBoMon} value={bm.maBoMon}>
                          {bm.tenBoMon}
                        </option>
                      ))}
                    </select>
                    <small className="text-muted d-block mt-1" style={{ fontSize: '0.75rem' }}>
                      * Trưởng bộ môn hoặc Giảng viên sẽ chỉ xem và quản lý dữ liệu liên quan đến bộ môn này.
                    </small>
                  </div>

                  {dangSuaId && (
                    <div className="mb-2">
                      <label className="form-label small fw-semibold">Trạng thái</label>
                      <select
                        className="form-select form-select-sm"
                        value={formTrangThai}
                        onChange={(e) => setFormTrangThai(e.target.value)}
                      >
                        <option value="KichHoat">Kích hoạt</option>
                        <option value="Khoa">Khóa tài khoản</option>
                      </select>
                    </div>
                  )}
                </div>
                <div className="modal-footer py-2">
                  <button
                    type="button"
                    className="btn btn-secondary btn-sm"
                    onClick={() => setHienModal(false)}
                  >
                    Hủy
                  </button>
                  <button type="submit" className="btn btn-primary btn-sm">
                    {dangSuaId ? 'Lưu thay đổi' : 'Tạo tài khoản'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {/* Modal Reset Mật khẩu */}
      {hienModalReset && (
        <div
          className="modal show d-block"
          style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}
        >
          <div className="modal-dialog modal-sm">
            <div className="modal-content">
              <form onSubmit={handleResetMatKhau}>
                <div className="modal-header py-2">
                  <h5 className="modal-title h6">Đặt lại mật khẩu</h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={() => setHienModalReset(false)}
                  />
                </div>
                <div className="modal-body">
                  <div className="mb-2">
                    <label className="form-label small fw-semibold">Mật khẩu mới</label>
                    <input
                      type="password"
                      className="form-control form-control-sm"
                      value={matKhauReset}
                      onChange={(e) => setMatKhauReset(e.target.value)}
                      placeholder="Nhập mật khẩu mới"
                      required
                      autoFocus
                    />
                  </div>
                </div>
                <div className="modal-footer py-2">
                  <button
                    type="button"
                    className="btn btn-secondary btn-sm"
                    onClick={() => setHienModalReset(false)}
                  >
                    Hủy
                  </button>
                  <button type="submit" className="btn btn-warning btn-sm">
                    Lưu mật khẩu
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}

export default DanhSachNguoiDung
