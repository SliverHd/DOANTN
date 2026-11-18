import { useEffect, useState, type FormEvent } from 'react'
import { hocKyApi } from '../../api/hocKyApi'
import { namHocApi } from '../../api/namHocApi'
import { IconEdit, IconTrash } from '../../components/ActionIcons'
import type { HocKy, TaoHocKyPayload } from '../../types/hocKy'
import type { NamHoc } from '../../types/namHoc'

function DanhSachHocKy() {
  const [danhSach, setDanhSach] = useState<HocKy[]>([])
  const [danhSachNamHoc, setDanhSachNamHoc] = useState<NamHoc[]>([])
  const [locNamHocId, setLocNamHocId] = useState<number>(0)
  const [dangTai, setDangTai] = useState(false)
  const [loi, setLoi] = useState('')
  const [thongBao, setThongBao] = useState('')
  const [modalLoi, setModalLoi] = useState('')

  const [hienModal, setHienModal] = useState(false)
  const [dangSuaId, setDangSuaId] = useState<number | null>(null)
  const [form, setForm] = useState<TaoHocKyPayload>({
    maNamHoc: 0,
    tenHocKy: '',
    ngayBatDau: '',
    ngayKetThuc: '',
    trangThai: 'DangApDung',
  })

  const taiDuLieu = async () => {
    try {
      setDangTai(true)
      const [resHocKy, resNamHoc] = await Promise.all([
        hocKyApi.getAll(),
        namHocApi.getAll(),
      ])
      setDanhSach(resHocKy.data)
      setDanhSachNamHoc(resNamHoc.data)
    } catch {
      setLoi('Không thể tải danh sách học kỳ hoặc năm học.')
    } finally {
      setDangTai(false)
    }
  }

  useEffect(() => {
    taiDuLieu()
  }, [])

  const moModalThem = () => {
    setDangSuaId(null)
    const defaultNamHocId = locNamHocId || (danhSachNamHoc.length > 0 ? danhSachNamHoc[0].maNamHoc : 0)
    setForm({
      maNamHoc: defaultNamHocId,
      tenHocKy: '',
      ngayBatDau: new Date().toISOString().substring(0, 10),
      ngayKetThuc: new Date(Date.now() + 120 * 24 * 60 * 60 * 1000).toISOString().substring(0, 10),
      trangThai: 'DangApDung',
    })
    setModalLoi('')
    setHienModal(true)
  }

  const moModalSua = (item: HocKy) => {
    setDangSuaId(item.maHocKy)
    setForm({
      maNamHoc: item.maNamHoc,
      tenHocKy: item.tenHocKy,
      ngayBatDau: item.ngayBatDau ? item.ngayBatDau.substring(0, 10) : '',
      ngayKetThuc: item.ngayKetThuc ? item.ngayKetThuc.substring(0, 10) : '',
      trangThai: item.trangThai,
    })
    setModalLoi('')
    setHienModal(true)
  }

  const handleLuu = async (e: FormEvent) => {
    e.preventDefault()
    setModalLoi('')
    setThongBao('')
    setLoi('')

    if (!form.maNamHoc) {
      setModalLoi('Vui lòng chọn năm học.')
      return
    }
    if (!form.tenHocKy.trim()) {
      setModalLoi('Vui lòng nhập tên học kỳ.')
      return
    }
    if (form.ngayBatDau && form.ngayKetThuc && form.ngayBatDau >= form.ngayKetThuc) {
      setModalLoi('Ngày bắt đầu phải trước ngày kết thúc.')
      return
    }

    try {
      if (dangSuaId) {
        await hocKyApi.update(dangSuaId, form)
        setThongBao('Cập nhật học kỳ thành công.')
      } else {
        await hocKyApi.create(form)
        setThongBao('Thêm học kỳ mới thành công.')
      }
      setHienModal(false)
      await taiDuLieu()
    } catch (err: unknown) {
      let msg = 'Lỗi khi lưu thông tin học kỳ.'
      if (typeof err === 'object' && err !== null && 'response' in err) {
        const axiosErr = err as { response?: { data?: string } }
        if (axiosErr.response?.data && typeof axiosErr.response.data === 'string') {
          msg = axiosErr.response.data
        }
      }
      setModalLoi(msg)
    }
  }

  const handleXoa = async (id: number) => {
    if (!confirm('Bạn có chắc chắn muốn xóa học kỳ này?')) return
    setLoi('')
    setThongBao('')
    try {
      await hocKyApi.delete(id)
      setThongBao('Xóa học kỳ thành công.')
      await taiDuLieu()
    } catch (err: unknown) {
      let msg = 'Không thể xóa học kỳ đang có lớp học phần hoặc dữ liệu liên kết.'
      if (typeof err === 'object' && err !== null && 'response' in err) {
        const axiosErr = err as { response?: { data?: string } }
        if (axiosErr.response?.data && typeof axiosErr.response.data === 'string') {
          msg = axiosErr.response.data
        }
      }
      setLoi(msg)
    }
  }

  const danhSachHienThi = locNamHocId
    ? danhSach.filter((x) => x.maNamHoc === locNamHocId)
    : danhSach

  const layTenNamHoc = (hk: HocKy) => {
    if (hk.tenNamHoc) return hk.tenNamHoc
    const nh = danhSachNamHoc.find((x) => x.maNamHoc === hk.maNamHoc)
    return nh ? nh.tenNamHoc : hk.maNamHoc
  }

  return (
    <section>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 className="h5 mb-1">Quản lý Học kỳ</h2>
          <p className="text-muted small mb-0">Thiết lập học kỳ và trạng thái áp dụng theo từng năm học</p>
        </div>
        <div className="d-flex gap-2">
          <select
            className="form-select form-select-sm"
            style={{ width: '180px' }}
            value={locNamHocId}
            onChange={(e) => setLocNamHocId(Number(e.target.value))}
          >
            <option value={0}>-- Tất cả năm học --</option>
            {danhSachNamHoc.map((nh) => (
              <option key={nh.maNamHoc} value={nh.maNamHoc}>
                {nh.tenNamHoc}
              </option>
            ))}
          </select>

          <button className="btn btn-primary btn-sm" onClick={moModalThem}>
            + Thêm học kỳ
          </button>
        </div>
      </div>

      {thongBao && <div className="alert alert-success py-2 small">{thongBao}</div>}
      {loi && <div className="alert alert-danger py-2 small">{loi}</div>}

      <div className="table-responsive border bg-white">
        <table className="table table-modern table-hover table-sm mb-0">
          <thead>
            <tr>
              <th style={{ width: '60px' }}>STT</th>
              <th>Tên học kỳ</th>
              <th>Năm học</th>
              <th>Ngày bắt đầu</th>
              <th>Ngày kết thúc</th>
              <th>Trạng thái</th>
              <th style={{ width: '130px' }} className="text-end">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {dangTai ? (
              <tr>
                <td colSpan={7} className="text-center py-3 text-muted">
                  Đang tải dữ liệu...
                </td>
              </tr>
            ) : danhSachHienThi.length === 0 ? (
              <tr>
                <td colSpan={7} className="text-center py-3 text-muted">
                  Chưa có học kỳ nào phù hợp.
                </td>
              </tr>
            ) : (
              danhSachHienThi.map((hk, idx) => (
                <tr key={hk.maHocKy}>
                  <td>{idx + 1}</td>
                  <td className="fw-semibold">{hk.tenHocKy}</td>
                  <td>{layTenNamHoc(hk)}</td>
                  <td>{hk.ngayBatDau ? hk.ngayBatDau.substring(0, 10) : '-'}</td>
                  <td>{hk.ngayKetThuc ? hk.ngayKetThuc.substring(0, 10) : '-'}</td>
                  <td>
                    <span
                      className={`badge-pill-status ${
                        hk.trangThai === 'DangApDung'
                          ? 'badge-pill-status--active'
                          : 'badge-pill-status--inactive'
                      }`}
                    >
                      {hk.trangThai === 'DangApDung' ? 'Đang áp dụng' : 'Kết thúc'}
                    </span>
                  </td>
                  <td className="text-end">
                    <div className="action-group justify-content-end">
                      <button
                        type="button"
                        className="btn-action btn-action--sm"
                        aria-label={`Sửa học kỳ ${hk.tenHocKy}`}
                        onClick={() => moModalSua(hk)}
                      >
                        <IconEdit />
                        <span>Sửa</span>
                      </button>
                      <button
                        type="button"
                        className="btn-action btn-action--sm btn-action--danger"
                        aria-label={`Xóa học kỳ ${hk.tenHocKy}`}
                        onClick={() => handleXoa(hk.maHocKy)}
                      >
                        <IconTrash />
                        <span>Xóa</span>
                      </button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Modal Them / Sua */}
      {hienModal && (
        <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog">
            <div className="modal-content">
              <form onSubmit={handleLuu}>
                <div className="modal-header py-2">
                  <h5 className="modal-title h6">
                    {dangSuaId ? 'Cập nhật học kỳ' : 'Thêm học kỳ mới'}
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={() => setHienModal(false)}
                  />
                </div>
                <div className="modal-body">
                  {modalLoi && <div className="alert alert-danger py-1 small">{modalLoi}</div>}
                  <div className="mb-3">
                    <label className="form-label small fw-semibold">Năm học *</label>
                    <select
                      className="form-select form-select-sm"
                      value={form.maNamHoc}
                      onChange={(e) => setForm({ ...form, maNamHoc: Number(e.target.value) })}
                      required
                    >
                      <option value={0}>-- Chọn năm học --</option>
                      {danhSachNamHoc.map((nh) => (
                        <option key={nh.maNamHoc} value={nh.maNamHoc}>
                          {nh.tenNamHoc}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-semibold">Tên học kỳ *</label>
                    <input
                      type="text"
                      className="form-control form-control-sm"
                      value={form.tenHocKy}
                      onChange={(e) => setForm({ ...form, tenHocKy: e.target.value })}
                      placeholder="Ví dụ: Học kỳ 1"
                      required
                    />
                  </div>

                  <div className="row g-2 mb-3">
                    <div className="col-6">
                      <label className="form-label small fw-semibold">Ngày bắt đầu</label>
                      <input
                        type="date"
                        className="form-control form-control-sm"
                        value={form.ngayBatDau}
                        onChange={(e) => setForm({ ...form, ngayBatDau: e.target.value })}
                        required
                      />
                    </div>
                    <div className="col-6">
                      <label className="form-label small fw-semibold">Ngày kết thúc</label>
                      <input
                        type="date"
                        className="form-control form-control-sm"
                        value={form.ngayKetThuc}
                        onChange={(e) => setForm({ ...form, ngayKetThuc: e.target.value })}
                        required
                      />
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-semibold">Trạng thái</label>
                    <select
                      className="form-select form-select-sm"
                      value={form.trangThai}
                      onChange={(e) => setForm({ ...form, trangThai: e.target.value })}
                    >
                      <option value="DangApDung">Đang áp dụng</option>
                      <option value="KetThuc">Kết thúc</option>
                    </select>
                  </div>
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
                    {dangSuaId ? 'Lưu thay đổi' : 'Thêm mới'}
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

export default DanhSachHocKy
