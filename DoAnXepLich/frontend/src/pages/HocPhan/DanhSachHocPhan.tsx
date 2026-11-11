import { useEffect, useState } from 'react'
import { hocPhanApi } from '../../api/hocPhanApi'
import { boMonApi } from '../../api/boMonApi'
import { hocKyApi } from '../../api/hocKyApi'
import Loading from '../../components/Loading'
import { useAuth } from '../../contexts/AuthContext'
import type { HocPhan, TaoHocPhanDto } from '../../types/hocPhan'
import type { BoMon } from '../../types/boMon'
import type { HocKy } from '../../types/hocKy'

function DanhSachHocPhan() {
  const { user } = useAuth()
  const laAdmin = user?.vaiTro === 'Admin'
  const maBoMonUser = user?.maBoMon

  const [danhSach, setDanhSach] = useState<HocPhan[]>([])
  const [dangTai, setDangTai] = useState(false)
  const [loi, setLoi] = useState('')
  const [tuKhoa, setTuKhoa] = useState('')

  // Bộ lọc Bộ môn & Học kỳ
  const [danhSachBoMon, setDanhSachBoMon] = useState<BoMon[]>([])
  const [danhSachHocKy, setDanhSachHocKy] = useState<HocKy[]>([])
  const [maBoMon, setMaBoMon] = useState<number>(!laAdmin && maBoMonUser ? maBoMonUser : 0)
  const [maHocKy, setMaHocKy] = useState<number>(0)

  // State Modal Thêm / Sửa
  const [hienModal, setHienModal] = useState(false)
  const [dangSuaId, setDangSuaId] = useState<number | null>(null)
  const [formData, setFormData] = useState<TaoHocPhanDto>({
    tenHocPhan: '',
    soTinChi: 3,
    trangThai: 'DangApDung',
    maBoMon: undefined,
  })
  const [dangLuu, setDangLuu] = useState(false)

  // Tải danh sách bộ môn và học kỳ ban đầu
  useEffect(() => {
    Promise.all([boMonApi.getAll(), hocKyApi.getAll()])
      .then(([bmRes, hkRes]) => {
        setDanhSachBoMon(bmRes.data)
        setDanhSachHocKy(hkRes.data)
      })
      .catch(() => {
        setLoi('Không thể tải danh sách bộ môn và học kỳ.')
      })
  }, [])

  // Chỉ gọi API học phần khi cả bộ môn và học kỳ đã được chọn
  useEffect(() => {
    if (maBoMon > 0 && maHocKy > 0) {
      void taiDuLieu(maBoMon, maHocKy)
    } else {
      setDanhSach([])
      setDangTai(false)
    }
  }, [maBoMon, maHocKy])

  const taiDuLieu = async (bmId: number, hkId: number) => {
    try {
      setDangTai(true)
      const res = await hocPhanApi.getAll(bmId, hkId)
      setDanhSach(res.data)
      setLoi('')
    } catch {
      setLoi('Không thể tải danh sách học phần.')
    } finally {
      setDangTai(false)
    }
  }

  const handleMoModalThem = () => {
    setDangSuaId(null)
    setFormData({
      tenHocPhan: '',
      soTinChi: 3,
      trangThai: 'DangApDung',
      maBoMon: maBoMon > 0 ? maBoMon : undefined,
    })
    setHienModal(true)
  }

  const handleMoModalSua = (hp: HocPhan) => {
    setDangSuaId(hp.maHocPhan)
    setFormData({
      tenHocPhan: hp.tenHocPhan,
      soTinChi: hp.soTinChi,
      trangThai: hp.trangThai || 'DangApDung',
      maBoMon: hp.maBoMon,
    })
    setHienModal(true)
  }

  const handleLuu = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!formData.tenHocPhan.trim()) {
      alert('Vui lòng nhập tên học phần!')
      return
    }

    try {
      setDangLuu(true)
      if (dangSuaId) {
        await hocPhanApi.update(dangSuaId, formData)
      } else {
        await hocPhanApi.create(formData)
      }
      setHienModal(false)
      if (maBoMon > 0 && maHocKy > 0) {
        await taiDuLieu(maBoMon, maHocKy)
      }
    } catch {
      alert('Không thể lưu học phần.')
    } finally {
      setDangLuu(false)
    }
  }

  const handleXoa = async (id: number, ten: string) => {
    if (!confirm(`Bạn có chắc chắn muốn xóa học phần "${ten}"?`)) return
    try {
      await hocPhanApi.delete(id)
      if (maBoMon > 0 && maHocKy > 0) {
        await taiDuLieu(maBoMon, maHocKy)
      }
    } catch {
      alert('Không thể xóa học phần này (có thể đã được dùng trong Lớp học phần hoặc Năng lực giảng dạy).')
    }
  }

  const danhSachLoc = danhSach.filter((hp) =>
    hp.tenHocPhan.toLowerCase().includes(tuKhoa.toLowerCase())
  )

  const daChonDuBoLoc = maBoMon > 0 && maHocKy > 0

  return (
    <section>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 className="h4 mb-1">Danh mục Học phần (Môn học)</h2>
          <p className="text-muted small mb-0">
            Quản lý danh sách các học phần theo từng Bộ môn và Học kỳ phục vụ phân công chuyên môn và thời khóa biểu
          </p>
        </div>
        <button
          className="btn btn-primary"
          onClick={handleMoModalThem}
          disabled={!daChonDuBoLoc}
          title={!daChonDuBoLoc ? 'Vui lòng chọn Bộ môn và Học kỳ trước' : undefined}
        >
          + Thêm học phần
        </button>
      </div>

      {/* Thanh chọn Bộ môn, Học kỳ và Tìm kiếm */}
      <div className="card p-3 mb-3 bg-light border-0">
        <div className="row g-2 align-items-end">
          <div className="col-md-3">
            <label className="form-label small fw-semibold text-secondary mb-1">
              Học kỳ <span className="text-danger">*</span>
            </label>
            <select
              className="form-select form-select-sm"
              value={maHocKy}
              onChange={(e) => setMaHocKy(Number(e.target.value))}
            >
              <option value={0}>-- Chọn học kỳ --</option>
              {danhSachHocKy.map((hk) => (
                <option key={hk.maHocKy} value={hk.maHocKy}>
                  {hk.tenHocKy}
                </option>
              ))}
            </select>
          </div>

          <div className="col-md-3">
            <label className="form-label small fw-semibold text-secondary mb-1">
              Bộ môn <span className="text-danger">*</span>
            </label>
            {laAdmin ? (
              <select
                className="form-select form-select-sm"
                value={maBoMon}
                onChange={(e) => setMaBoMon(Number(e.target.value))}
              >
                <option value={0}>-- Chọn bộ môn --</option>
                {danhSachBoMon.map((bm) => (
                  <option key={bm.maBoMon} value={bm.maBoMon}>
                    {bm.tenBoMon}
                  </option>
                ))}
              </select>
            ) : (
              <div className="form-control form-control-sm bg-white text-primary fw-semibold">
                {user?.tenBoMon || 'Chưa gán bộ môn'}
              </div>
            )}
          </div>

          <div className="col-md-6">
            <label className="form-label small fw-semibold text-secondary mb-1">Tìm kiếm</label>
            <input
              type="text"
              className="form-control form-control-sm"
              placeholder="Tìm theo tên học phần..."
              value={tuKhoa}
              onChange={(e) => setTuKhoa(e.target.value)}
              disabled={!daChonDuBoLoc}
            />
          </div>
        </div>
      </div>

      {loi && <div className="alert alert-warning py-2">{loi}</div>}

      {!daChonDuBoLoc ? (
        <div className="card text-center p-5 bg-white border">
          <div className="text-muted mb-2">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              width="48"
              height="48"
              fill="currentColor"
              className="bi bi-filter-circle text-primary opacity-50 mb-2"
              viewBox="0 0 16 16"
            >
              <path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14m0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16" />
              <path d="M7 11.5a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 0 1h-1a.5.5 0 0 1-.5-.5m-2-3a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5m-2-3a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5" />
            </svg>
          </div>
          <h5 className="text-secondary">Chưa chọn Bộ môn hoặc Học kỳ</h5>
          <p className="text-muted small mb-0">
            Vui lòng chọn cả <strong>Học kỳ</strong> và <strong>Bộ môn</strong> ở trên để tải danh sách các môn học tương ứng.
          </p>
        </div>
      ) : dangTai ? (
        <Loading />
      ) : (
        <div className="card shadow-sm">
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead className="table-light">
                <tr>
                  <th style={{ width: '80px' }}>Mã HP</th>
                  <th>Tên học phần</th>
                  <th style={{ width: '120px' }}>Số tín chỉ</th>
                  <th style={{ width: '180px' }}>Bộ môn phụ trách</th>
                  <th style={{ width: '140px' }}>Trạng thái</th>
                  <th style={{ width: '130px' }} className="text-center">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {danhSachLoc.map((hp) => (
                  <tr key={hp.maHocPhan}>
                    <td>
                      <span className="badge bg-light text-dark border">#{hp.maHocPhan}</span>
                    </td>
                    <td>
                      <strong>{hp.tenHocPhan}</strong>
                    </td>
                    <td>
                      <span className="badge bg-info text-dark">{hp.soTinChi} TC</span>
                    </td>
                    <td>
                      <span className="badge bg-light text-secondary border">
                        {hp.tenBoMon || 'Chưa gán bộ môn'}
                      </span>
                    </td>
                    <td>
                      <span
                        className={`badge ${hp.trangThai === 'DangApDung' ? 'bg-success' : 'bg-secondary'
                          }`}
                      >
                        {hp.trangThai === 'DangApDung' ? 'Đang áp dụng' : hp.trangThai}
                      </span>
                    </td>
                    <td className="text-center">
                      <div className="btn-group btn-group-sm">
                        <button
                          className="btn btn-outline-secondary"
                          onClick={() => handleMoModalSua(hp)}
                        >
                          Sửa
                        </button>
                        <button
                          className="btn btn-outline-danger"
                          onClick={() => handleXoa(hp.maHocPhan, hp.tenHocPhan)}
                        >
                          Xóa
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {danhSachLoc.length === 0 && (
                  <tr>
                    <td colSpan={6} className="text-center text-secondary py-4">
                      Không tìm thấy học phần nào của bộ môn trong học kỳ này.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Modal Thêm / Sửa */}
      {hienModal && (
        <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog">
            <div className="modal-content">
              <form onSubmit={handleLuu}>
                <div className="modal-header">
                  <h5 className="modal-title">
                    {dangSuaId ? 'Cập nhật học phần' : 'Thêm học phần mới'}
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
                      Tên học phần <span className="text-danger">*</span>
                    </label>
                    <input
                      type="text"
                      className="form-control"
                      required
                      value={formData.tenHocPhan}
                      onChange={(e) => setFormData({ ...formData, tenHocPhan: e.target.value })}
                      placeholder="VD: Cơ sở dữ liệu, Lập trình Web..."
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">Số tín chỉ</label>
                    <input
                      type="number"
                      min={1}
                      max={10}
                      className="form-control"
                      value={formData.soTinChi}
                      onChange={(e) => setFormData({ ...formData, soTinChi: Number(e.target.value) })}
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">Bộ môn phụ trách</label>
                    <select
                      className="form-select"
                      value={formData.maBoMon || 0}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          maBoMon: Number(e.target.value) > 0 ? Number(e.target.value) : undefined,
                        })
                      }
                    >
                      <option value={0}>-- Chọn bộ môn --</option>
                      {danhSachBoMon.map((bm) => (
                        <option key={bm.maBoMon} value={bm.maBoMon}>
                          {bm.tenBoMon}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="mb-3">
                    <label className="form-label">Trạng thái</label>
                    <select
                      className="form-select"
                      value={formData.trangThai}
                      onChange={(e) => setFormData({ ...formData, trangThai: e.target.value })}
                    >
                      <option value="DangApDung">Đang áp dụng</option>
                      <option value="TamNgung">Tạm ngưng</option>
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
                    {dangLuu ? 'Đang lưu...' : 'Lưu học phần'}
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

export default DanhSachHocPhan
