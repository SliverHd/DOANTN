import { useEffect, useState } from 'react'
import { giangVienApi } from '../../api/giangVienApi'
import { boMonApi } from '../../api/boMonApi'
import { hocPhanApi } from '../../api/hocPhanApi'
import Loading from '../../components/Loading'
import type { GiangVien, TaoGiangVienDto } from '../../types/giangVien'
import type { BoMon } from '../../types/boMon'
import type { HocPhan } from '../../types/hocPhan'

function DanhSachGiangVien() {
  const [danhSach, setDanhSach] = useState<GiangVien[]>([])
  const [danhSachBoMon, setDanhSachBoMon] = useState<BoMon[]>([])
  const [danhSachHocPhan, setDanhSachHocPhan] = useState<HocPhan[]>([])
  const [dangTai, setDangTai] = useState(true)
  const [loi, setLoi] = useState('')
  const [tuKhoa, setTuKhoa] = useState('')
  const [locBoMon, setLocBoMon] = useState<number | ''>('')

  // Modal Thêm / Sửa GV
  const [hienModal, setHienModal] = useState(false)
  const [dangSuaId, setDangSuaId] = useState<number | null>(null)
  const [formData, setFormData] = useState<TaoGiangVienDto>({
    hoTen: '',
    email: '',
    soDienThoai: '',
    chucDanh: 'Giảng viên',
    maBoMon: undefined,
    trangThai: 'DangLamViec',
  })
  const [dangLuu, setDangLuu] = useState(false)

  // Modal Năng lực môn dạy
  const [hienModalNangLuc, setHienModalNangLuc] = useState(false)
  const [gvDangChon, setGvDangChon] = useState<GiangVien | null>(null)
  const [monChonIds, setMonChonIds] = useState<number[]>([])
  const [tuKhoaMon, setTuKhoaMon] = useState('')
  const [dangLuuNangLuc, setDangLuuNangLuc] = useState(false)

  const taiDuLieu = async () => {
    try {
      setDangTai(true)
      const [resGV, resBM, resHP] = await Promise.all([
        giangVienApi.getAll(),
        boMonApi.getAll().catch(() => ({ data: [] as BoMon[] })),
        hocPhanApi.getAll().catch(() => ({ data: [] as HocPhan[] })),
      ])
      setDanhSach(resGV.data)
      setDanhSachBoMon(resBM.data)
      setDanhSachHocPhan(resHP.data)
      setLoi('')
    } catch {
      setLoi('Không thể tải danh sách giảng viên.')
    } finally {
      setDangTai(false)
    }
  }

  useEffect(() => {
    void taiDuLieu()
  }, [])

  const handleMoModalThem = () => {
    setDangSuaId(null)
    setFormData({
      hoTen: '',
      email: '',
      soDienThoai: '',
      chucDanh: 'Giảng viên',
      maBoMon: danhSachBoMon.length > 0 ? danhSachBoMon[0].maBoMon : undefined,
      trangThai: 'DangLamViec',
    })
    setHienModal(true)
  }

  const handleMoModalSua = (gv: GiangVien) => {
    setDangSuaId(gv.maGiangVien)
    setFormData({
      hoTen: gv.hoTen,
      email: gv.email,
      soDienThoai: gv.soDienThoai || '',
      chucDanh: gv.chucDanh || 'Giảng viên',
      maBoMon: gv.maBoMon,
      trangThai: gv.trangThai || 'DangLamViec',
    })
    setHienModal(true)
  }

  const handleLuuGiangVien = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!formData.hoTen.trim() || !formData.email.trim()) {
      alert('Vui lòng nhập họ tên và email!')
      return
    }

    try {
      setDangLuu(true)
      if (dangSuaId) {
        await giangVienApi.update(dangSuaId, formData)
      } else {
        await giangVienApi.create(formData)
      }
      setHienModal(false)
      await taiDuLieu()
    } catch (err: unknown) {
      alert('Không thể lưu thông tin giảng viên.')
    } finally {
      setDangLuu(false)
    }
  }

  const handleXoaGiangVien = async (id: number, hoTen: string) => {
    if (!confirm(`Bạn có chắc chắn muốn xóa giảng viên "${hoTen}"?`)) return
    try {
      await giangVienApi.delete(id)
      await taiDuLieu()
    } catch {
      alert('Không thể xóa giảng viên này (có thể đã có dữ liệu ràng buộc liên quan).')
    }
  }

  // Quản lý năng lực môn dạy
  const handleMoModalNangLuc = (gv: GiangVien) => {
    setGvDangChon(gv)
    const currentMonIds = gv.danhSachNangLuc?.map((x) => x.maHocPhan) || []
    setMonChonIds(currentMonIds)
    setTuKhoaMon('')
    setHienModalNangLuc(true)
  }

  const handleToggleMon = (maHp: number) => {
    setMonChonIds((prev) =>
      prev.includes(maHp) ? prev.filter((id) => id !== maHp) : [...prev, maHp]
    )
  }

  const handleLuuNangLuc = async () => {
    if (!gvDangChon) return
    try {
      setDangLuuNangLuc(true)
      await giangVienApi.capNhatNangLuc(gvDangChon.maGiangVien, monChonIds)
      setHienModalNangLuc(false)
      await taiDuLieu()
    } catch {
      alert('Không thể cập nhật danh sách năng lực môn dạy.')
    } finally {
      setDangLuuNangLuc(false)
    }
  }

  const danhSachLoc = danhSach.filter((gv) => {
    const khopTuKhoa =
      gv.hoTen.toLowerCase().includes(tuKhoa.toLowerCase()) ||
      gv.email.toLowerCase().includes(tuKhoa.toLowerCase()) ||
      (gv.chucDanh && gv.chucDanh.toLowerCase().includes(tuKhoa.toLowerCase()))
    const khopBoMon = locBoMon === '' || gv.maBoMon === locBoMon
    return khopTuKhoa && khopBoMon
  })

  return (
    <section>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 className="h4 mb-1">Quản lý Giảng viên & Năng lực chuyên môn</h2>
          <p className="text-muted small mb-0">
            Quản lý danh sách giảng viên, bộ môn trực thuộc và các học phần có chuyên môn giảng dạy
          </p>
        </div>
        <button className="btn btn-primary" onClick={handleMoModalThem}>
          <i className="bi bi-plus-lg me-1"></i> + Thêm giảng viên
        </button>
      </div>

      {/* Thanh tìm kiếm & lọc */}
      <div className="row g-2 mb-3">
        <div className="col-md-6">
          <input
            type="text"
            className="form-control"
            placeholder="Tìm theo tên, email, chức danh..."
            value={tuKhoa}
            onChange={(e) => setTuKhoa(e.target.value)}
          />
        </div>
        <div className="col-md-4">
          <select
            className="form-select"
            value={locBoMon}
            onChange={(e) => setLocBoMon(e.target.value ? Number(e.target.value) : '')}
          >
            <option value="">-- Tất cả Bộ môn --</option>
            {danhSachBoMon.map((bm) => (
              <option key={bm.maBoMon} value={bm.maBoMon}>
                {bm.tenBoMon}
              </option>
            ))}
          </select>
        </div>
      </div>

      {dangTai && <Loading />}
      {loi && <div className="alert alert-warning">{loi}</div>}

      {!dangTai && !loi && (
        <div className="card shadow-sm">
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead className="table-light">
                <tr>
                  <th style={{ width: '50px' }}>STT</th>
                  <th>Họ và tên</th>
                  <th>Email / SĐT</th>
                  <th>Chức danh</th>
                  <th>Bộ môn</th>
                  <th>Năng lực môn dạy</th>
                  <th>Trạng thái</th>
                  <th style={{ width: '170px' }} className="text-center">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {danhSachLoc.map((gv, idx) => (
                  <tr key={gv.maGiangVien}>
                    <td>{idx + 1}</td>
                    <td>
                      <strong>{gv.hoTen}</strong>
                    </td>
                    <td>
                      <div>{gv.email}</div>
                      {gv.soDienThoai && <small className="text-muted">{gv.soDienThoai}</small>}
                    </td>
                    <td>{gv.chucDanh || 'Giảng viên'}</td>
                    <td>
                      {gv.tenBoMon ? (
                        <span className="badge bg-secondary">{gv.tenBoMon}</span>
                      ) : (
                        <span className="text-muted small">Chưa gán</span>
                      )}
                    </td>
                    <td>
                      {gv.danhSachNangLuc && gv.danhSachNangLuc.length > 0 ? (
                        <div className="d-flex flex-wrap gap-1">
                          {gv.danhSachNangLuc.slice(0, 3).map((nl) => (
                            <span key={nl.maNangLuc} className="badge bg-info text-dark">
                              {nl.tenHocPhan}
                            </span>
                          ))}
                          {gv.danhSachNangLuc.length > 3 && (
                            <span className="badge bg-light text-muted border">
                              +{gv.danhSachNangLuc.length - 3} môn khác
                            </span>
                          )}
                        </div>
                      ) : (
                        <span className="text-danger small">Chưa gán môn</span>
                      )}
                    </td>
                    <td>
                      <span
                        className={`badge ${
                          gv.trangThai === 'DangLamViec' || gv.trangThai === 'DangHoatDong'
                            ? 'bg-success'
                            : 'bg-warning text-dark'
                        }`}
                      >
                        {gv.trangThai === 'DangLamViec' ? 'Đang làm việc' : gv.trangThai}
                      </span>
                    </td>
                    <td className="text-center">
                      <div className="btn-group btn-group-sm">
                        <button
                          className="btn btn-outline-info"
                          title="Gán năng lực môn dạy"
                          onClick={() => handleMoModalNangLuc(gv)}
                        >
                          Môn dạy
                        </button>
                        <button
                          className="btn btn-outline-secondary"
                          title="Sửa thông tin"
                          onClick={() => handleMoModalSua(gv)}
                        >
                          Sửa
                        </button>
                        <button
                          className="btn btn-outline-danger"
                          title="Xóa giảng viên"
                          onClick={() => handleXoaGiangVien(gv.maGiangVien, gv.hoTen)}
                        >
                          Xóa
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {danhSachLoc.length === 0 && (
                  <tr>
                    <td colSpan={8} className="text-center text-secondary py-4">
                      Không tìm thấy giảng viên nào.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Modal 1: Thêm / Sửa Giảng viên */}
      {hienModal && (
        <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog">
            <div className="modal-content">
              <form onSubmit={handleLuuGiangVien}>
                <div className="modal-header">
                  <h5 className="modal-title">
                    {dangSuaId ? 'Cập nhật thông tin giảng viên' : 'Thêm giảng viên mới'}
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={() => setHienModal(false)}
                  ></button>
                </div>
                <div className="modal-body">
                  <div className="mb-3">
                    <label className="form-label">
                      Họ và tên <span className="text-danger">*</span>
                    </label>
                    <input
                      type="text"
                      className="form-control"
                      required
                      value={formData.hoTen}
                      onChange={(e) => setFormData({ ...formData, hoTen: e.target.value })}
                      placeholder="VD: TS. Nguyễn Văn A"
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">
                      Email <span className="text-danger">*</span>
                    </label>
                    <input
                      type="email"
                      className="form-control"
                      required
                      value={formData.email}
                      onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                      placeholder="VD: nguyenvana@huce.edu.vn"
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">Số điện thoại</label>
                    <input
                      type="text"
                      className="form-control"
                      value={formData.soDienThoai}
                      onChange={(e) => setFormData({ ...formData, soDienThoai: e.target.value })}
                      placeholder="VD: 0912345678"
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">Chức danh / Học vị</label>
                    <input
                      type="text"
                      className="form-control"
                      value={formData.chucDanh}
                      onChange={(e) => setFormData({ ...formData, chucDanh: e.target.value })}
                      placeholder="VD: Giảng viên, Phó giáo sư, Trưởng bộ môn..."
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">Bộ môn trực thuộc</label>
                    <select
                      className="form-select"
                      value={formData.maBoMon ?? ''}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          maBoMon: e.target.value ? Number(e.target.value) : undefined,
                        })
                      }
                    >
                      <option value="">-- Chưa chọn bộ môn --</option>
                      {danhSachBoMon.map((bm) => (
                        <option key={bm.maBoMon} value={bm.maBoMon}>
                          {bm.tenBoMon}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="mb-3">
                    <label className="form-label">Trạng thái làm việc</label>
                    <select
                      className="form-select"
                      value={formData.trangThai}
                      onChange={(e) => setFormData({ ...formData, trangThai: e.target.value })}
                    >
                      <option value="DangLamViec">Đang làm việc</option>
                      <option value="TamNghi">Tạm nghỉ</option>
                      <option value="NghiViec">Nghỉ việc</option>
                    </select>
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => setHienModal(false)}
                  >
                    Hủy
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={dangLuu}>
                    {dangLuu ? 'Đang lưu...' : 'Lưu thông tin'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {/* Modal 2: Quản lý Năng lực môn dạy (Chuyên môn) */}
      {hienModalNangLuc && gvDangChon && (
        <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-lg">
            <div className="modal-content">
              <div className="modal-header">
                <div>
                  <h5 className="modal-title">Năng lực môn dạy chuyên môn</h5>
                  <small className="text-muted">
                    Giảng viên: <strong>{gvDangChon.hoTen}</strong> ({gvDangChon.email})
                  </small>
                </div>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setHienModalNangLuc(false)}
                ></button>
              </div>
              <div className="modal-body">

                <div className="d-flex justify-content-between align-items-center mb-3">
                  <input
                    type="text"
                    className="form-control w-50"
                    placeholder="Tìm học phần..."
                    value={tuKhoaMon}
                    onChange={(e) => setTuKhoaMon(e.target.value)}
                  />
                  <div className="btn-group btn-group-sm">
                    <button
                      type="button"
                      className="btn btn-outline-primary"
                      onClick={() => setMonChonIds(danhSachHocPhan.map((hp) => hp.maHocPhan))}
                    >
                      Chọn tất cả
                    </button>
                    <button
                      type="button"
                      className="btn btn-outline-secondary"
                      onClick={() => setMonChonIds([])}
                    >
                      Bỏ chọn tất cả
                    </button>
                  </div>
                </div>

                <div
                  className="border rounded p-2"
                  style={{ maxHeight: '350px', overflowY: 'auto' }}
                >
                  <div className="row g-2">
                    {danhSachHocPhan
                      .filter((hp) =>
                        hp.tenHocPhan.toLowerCase().includes(tuKhoaMon.toLowerCase())
                      )
                      .map((hp) => {
                        const daChon = monChonIds.includes(hp.maHocPhan)
                        return (
                          <div className="col-md-6" key={hp.maHocPhan}>
                            <div
                              className={`p-2 border rounded cursor-pointer ${
                                daChon ? 'bg-primary bg-opacity-10 border-primary' : 'bg-light'
                              }`}
                              style={{ cursor: 'pointer' }}
                              onClick={() => handleToggleMon(hp.maHocPhan)}
                            >
                              <div className="form-check">
                                <input
                                  className="form-check-input"
                                  type="checkbox"
                                  id={`hp-${hp.maHocPhan}`}
                                  checked={daChon}
                                  onChange={() => handleToggleMon(hp.maHocPhan)}
                                />
                                <label
                                  className="form-check-label fw-bold text-dark"
                                  htmlFor={`hp-${hp.maHocPhan}`}
                                  style={{ cursor: 'pointer' }}
                                >
                                  {hp.tenHocPhan}
                                </label>
                              </div>
                              <div className="small text-muted ms-4">
                                Số tín chỉ: {hp.soTinChi} TC
                              </div>
                            </div>
                          </div>
                        )
                      })}
                    {danhSachHocPhan.length === 0 && (
                      <div className="p-3 text-center text-muted">
                        Chưa có học phần nào trong hệ thống. Vui lòng thêm học phần trước.
                      </div>
                    )}
                  </div>
                </div>

                <div className="mt-2 text-muted small">
                  Đã chọn: <strong>{monChonIds.length}</strong> học phần
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setHienModalNangLuc(false)}
                >
                  Đóng
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  disabled={dangLuuNangLuc}
                  onClick={handleLuuNangLuc}
                >
                  {dangLuuNangLuc ? 'Đang lưu...' : 'Lưu danh mục môn dạy'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}

export default DanhSachGiangVien
