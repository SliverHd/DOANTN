import { useEffect, useState, type FormEvent } from 'react'
import { boMonApi } from '../../api/boMonApi'
import { IconEdit, IconTrash } from '../../components/ActionIcons'
import type { BoMon, TaoBoMonPayload } from '../../types/boMon'

function DanhSachBoMon() {
  const [danhSach, setDanhSach] = useState<BoMon[]>([])
  const [dangTai, setDangTai] = useState(false)
  const [loi, setLoi] = useState('')
  const [thongBao, setThongBao] = useState('')
  const [modalLoi, setModalLoi] = useState('')

  const [hienModal, setHienModal] = useState(false)
  const [dangSuaId, setDangSuaId] = useState<number | null>(null)
  const [form, setForm] = useState<TaoBoMonPayload>({
    tenBoMon: '',
    tenKhoa: '',
  })

  const taiDuLieu = async () => {
    try {
      setDangTai(true)
      const res = await boMonApi.getAll()
      setDanhSach(res.data)
    } catch {
      setLoi('Không thể tải danh sách bộ môn.')
    } finally {
      setDangTai(false)
    }
  }

  useEffect(() => {
    taiDuLieu()
  }, [])

  const moModalThem = () => {
    setDangSuaId(null)
    setForm({ tenBoMon: '', tenKhoa: '' })
    setModalLoi('')
    setHienModal(true)
  }

  const moModalSua = (item: BoMon) => {
    setDangSuaId(item.maBoMon)
    setForm({ tenBoMon: item.tenBoMon, tenKhoa: item.tenKhoa })
    setModalLoi('')
    setHienModal(true)
  }

  const handleLuu = async (e: FormEvent) => {
    e.preventDefault()
    setModalLoi('')
    setThongBao('')
    setLoi('')

    if (!form.tenBoMon.trim()) {
      setModalLoi('Vui lòng nhập tên bộ môn.')
      return
    }

    try {
      if (dangSuaId) {
        await boMonApi.update(dangSuaId, form)
        setThongBao('Cập nhật bộ môn thành công.')
      } else {
        await boMonApi.create(form)
        setThongBao('Thêm bộ môn mới thành công.')
      }
      setHienModal(false)
      await taiDuLieu()
    } catch (err: unknown) {
      let msg = 'Lỗi khi lưu thông tin bộ môn.'
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
    if (!confirm('Bạn có chắc chắn muốn xóa bộ môn này?')) return
    setLoi('')
    setThongBao('')
    try {
      await boMonApi.delete(id)
      setThongBao('Xóa bộ môn thành công.')
      await taiDuLieu()
    } catch (err: unknown) {
      let msg = 'Không thể xóa bộ môn đang có giảng viên hoặc học phần trực thuộc.'
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
          <h2 className="h5 mb-1">Quản lý Bộ môn</h2>
          <p className="text-muted small mb-0">Danh sách các bộ môn và khoa trực thuộc</p>
        </div>
        <button className="btn btn-primary btn-sm" onClick={moModalThem}>
          + Thêm bộ môn
        </button>
      </div>

      {thongBao && <div className="alert alert-success py-2 small">{thongBao}</div>}
      {loi && <div className="alert alert-danger py-2 small">{loi}</div>}

      <div className="table-responsive border bg-white">
        <table className="table table-modern table-hover table-sm mb-0">
          <thead>
            <tr>
              <th style={{ width: '60px' }}>STT</th>
              <th>Tên bộ môn</th>
              <th>Khoa</th>
              <th style={{ width: '130px' }} className="text-end">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {dangTai ? (
              <tr>
                <td colSpan={4} className="text-center py-3 text-muted">
                  Đang tải dữ liệu...
                </td>
              </tr>
            ) : danhSach.length === 0 ? (
              <tr>
                <td colSpan={4} className="text-center py-3 text-muted">
                  Chưa có bộ môn nào.
                </td>
              </tr>
            ) : (
              danhSach.map((bm, idx) => (
                <tr key={bm.maBoMon}>
                  <td>{idx + 1}</td>
                  <td>{bm.tenBoMon}</td>
                  <td>{bm.tenKhoa || <span className="text-muted">Chưa có</span>}</td>
                  <td className="text-end">
                    <div className="action-group justify-content-end">
                      <button
                        type="button"
                        className="btn-action btn-action--sm"
                        aria-label={`Sửa bộ môn ${bm.tenBoMon}`}
                        onClick={() => moModalSua(bm)}
                      >
                        <IconEdit />
                        <span>Sửa</span>
                      </button>
                      <button
                        type="button"
                        className="btn-action btn-action--sm btn-action--danger"
                        aria-label={`Xóa bộ môn ${bm.tenBoMon}`}
                        onClick={() => handleXoa(bm.maBoMon)}
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
                    {dangSuaId ? 'Cập nhật bộ môn' : 'Thêm bộ môn mới'}
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
                    <label className="form-label small fw-semibold">Tên bộ môn *</label>
                    <input
                      type="text"
                      className="form-control form-control-sm"
                      value={form.tenBoMon}
                      onChange={(e) => setForm({ ...form, tenBoMon: e.target.value })}
                      placeholder="Ví dụ: Công nghệ phần mềm"
                      required
                      autoFocus
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label small fw-semibold">Khoa</label>
                    <input
                      type="text"
                      className="form-control form-control-sm"
                      value={form.tenKhoa}
                      onChange={(e) => setForm({ ...form, tenKhoa: e.target.value })}
                      placeholder="Ví dụ: Công nghệ thông tin"
                    />
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

export default DanhSachBoMon
