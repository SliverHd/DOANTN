import { useEffect, useState, type FormEvent } from 'react'
import { namHocApi } from '../../api/namHocApi'
import { IconEdit, IconTrash } from '../../components/ActionIcons'
import type { NamHoc, TaoNamHocPayload } from '../../types/namHoc'

function DanhSachNamHoc() {
  const [danhSach, setDanhSach] = useState<NamHoc[]>([])
  const [dangTai, setDangTai] = useState(false)
  const [loi, setLoi] = useState('')
  const [thongBao, setThongBao] = useState('')
  const [modalLoi, setModalLoi] = useState('')

  const [hienModal, setHienModal] = useState(false)
  const [dangSuaId, setDangSuaId] = useState<number | null>(null)
  const [form, setForm] = useState<TaoNamHocPayload>({
    tenNamHoc: '',
    ngayBatDau: '',
    ngayKetThuc: '',
    trangThai: 'DangApDung',
  })

  const taiDuLieu = async () => {
    try {
      setDangTai(true)
      const res = await namHocApi.getAll()
      setDanhSach(res.data)
    } catch {
      setLoi('Không thể tải danh sách năm học.')
    } finally {
      setDangTai(false)
    }
  }

  useEffect(() => {
    taiDuLieu()
  }, [])

  const moModalThem = () => {
    setDangSuaId(null)
    setForm({
      tenNamHoc: '',
      ngayBatDau: new Date().toISOString().substring(0, 10),
      ngayKetThuc: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString().substring(0, 10),
      trangThai: 'DangApDung',
    })
    setModalLoi('')
    setHienModal(true)
  }

  const moModalSua = (item: NamHoc) => {
    setDangSuaId(item.maNamHoc)
    setForm({
      tenNamHoc: item.tenNamHoc,
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

    if (!form.tenNamHoc.trim()) {
      setModalLoi('Vui lòng nhập tên năm học.')
      return
    }

    if (form.ngayBatDau && form.ngayKetThuc && form.ngayBatDau >= form.ngayKetThuc) {
      setModalLoi('Ngày bắt đầu phải trước ngày kết thúc.')
      return
    }

    try {
      if (dangSuaId) {
        await namHocApi.update(dangSuaId, form)
        setThongBao('Cập nhật năm học thành công.')
      } else {
        await namHocApi.create(form)
        setThongBao('Thêm năm học mới thành công.')
      }
      setHienModal(false)
      await taiDuLieu()
    } catch (err: unknown) {
      let msg = 'Lỗi khi lưu thông tin năm học.'
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
    if (!confirm('Bạn có chắc chắn muốn xóa năm học này?')) return
    setLoi('')
    setThongBao('')
    try {
      await namHocApi.delete(id)
      setThongBao('Xóa năm học thành công.')
      await taiDuLieu()
    } catch (err: unknown) {
      let msg = 'Không thể xóa năm học đang có học kỳ trực thuộc.'
      if (typeof err === 'object' && err !== null && 'response' in err) {
        const axiosErr = err as { response?: { data?: string } }
        if (axiosErr.response?.data && typeof axiosErr.response.data === 'string') {
          msg = axiosErr.response.data
        }
      }
      setLoi(msg)
    }
  }

  return (
    <section>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 className="h5 mb-1">Quản lý Năm học</h2>
          <p className="text-muted small mb-0">Thiết lập các năm học trong chương trình đào tạo</p>
        </div>
        <button className="btn btn-primary btn-sm" onClick={moModalThem}>
          + Thêm năm học
        </button>
      </div>

      {thongBao && <div className="alert alert-success py-2 small">{thongBao}</div>}
      {loi && <div className="alert alert-danger py-2 small">{loi}</div>}

      <div className="table-responsive border bg-white">
        <table className="table table-modern table-hover table-sm mb-0">
          <thead>
            <tr>
              <th style={{ width: '60px' }}>STT</th>
              <th>Tên năm học</th>
              <th>Ngày bắt đầu</th>
              <th>Ngày kết thúc</th>
              <th>Trạng thái</th>
              <th style={{ width: '130px' }} className="text-end">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {dangTai ? (
              <tr>
                <td colSpan={6} className="text-center py-3 text-muted">
                  Đang tải dữ liệu...
                </td>
              </tr>
            ) : danhSach.length === 0 ? (
              <tr>
                <td colSpan={6} className="text-center py-3 text-muted">
                  Chưa có năm học nào.
                </td>
              </tr>
            ) : (
              danhSach.map((nh, idx) => (
                <tr key={nh.maNamHoc}>
                  <td>{idx + 1}</td>
                  <td className="fw-semibold">{nh.tenNamHoc}</td>
                  <td>{nh.ngayBatDau ? nh.ngayBatDau.substring(0, 10) : '-'}</td>
                  <td>{nh.ngayKetThuc ? nh.ngayKetThuc.substring(0, 10) : '-'}</td>
                  <td>
                    <span
                      className={`badge-pill-status ${
                        nh.trangThai === 'DangApDung'
                          ? 'badge-pill-status--active'
                          : 'badge-pill-status--inactive'
                      }`}
                    >
                      {nh.trangThai === 'DangApDung' ? 'Đang áp dụng' : 'Kết thúc'}
                    </span>
                  </td>
                  <td className="text-end">
                    <div className="action-group justify-content-end">
                      <button
                        type="button"
                        className="btn-action btn-action--sm"
                        aria-label={`Sửa năm học ${nh.tenNamHoc}`}
                        onClick={() => moModalSua(nh)}
                      >
                        <IconEdit />
                        <span>Sửa</span>
                      </button>
                      <button
                        type="button"
                        className="btn-action btn-action--sm btn-action--danger"
                        aria-label={`Xóa năm học ${nh.tenNamHoc}`}
                        onClick={() => handleXoa(nh.maNamHoc)}
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
                    {dangSuaId ? 'Cập nhật năm học' : 'Thêm năm học mới'}
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
                    <label className="form-label small fw-semibold">Tên năm học *</label>
                    <input
                      type="text"
                      className="form-control form-control-sm"
                      value={form.tenNamHoc}
                      onChange={(e) => setForm({ ...form, tenNamHoc: e.target.value })}
                      placeholder="Ví dụ: 2024-2025"
                      required
                      autoFocus
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

export default DanhSachNamHoc
