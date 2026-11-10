import { useEffect, useState } from 'react'
import { hocKyApi } from '../../api/hocKyApi'
import { boMonApi } from '../../api/boMonApi'
import { thoiKhoaBieuApi } from '../../api/thoiKhoaBieuApi'
import { useAuth } from '../../contexts/AuthContext'
import type { HocKy } from '../../types/hocKy'
import type { BoMon } from '../../types/boMon'
import type { LopHocPhan } from '../../types/lopHocPhan'
import ImportThoiKhoaBieu from './ImportThoiKhoaBieu'

function DanhSachLopHocPhan() {
  const { user } = useAuth()
  const isAdmin = user?.vaiTro === 'Admin'

  const [danhSachHocKy, setDanhSachHocKy] = useState<HocKy[]>([])
  const [danhSachBoMon, setDanhSachBoMon] = useState<BoMon[]>([])
  const [maHocKy, setMaHocKy] = useState<number>(0)
  const [maBoMon, setMaBoMon] = useState<number>(user?.maBoMon || 0)
  const [danhSachLop, setDanhSachLop] = useState<LopHocPhan[]>([])
  const [tuKhoa, setTuKhoa] = useState<string>('')
  const [dangTai, setDangTai] = useState(false)
  const [tabHienTai, setTabHienTai] = useState<'danhSach' | 'import'>('danhSach')

  const dsLopHienThi = danhSachLop.filter((lop) => {
    if (!tuKhoa.trim()) return true
    const kw = tuKhoa.toLowerCase()
    return (
      lop.maLopHocPhanTruong.toLowerCase().includes(kw) ||
      lop.tenLop.toLowerCase().includes(kw) ||
      lop.tenHocPhan.toLowerCase().includes(kw) ||
      (lop.tenBoMon || '').toLowerCase().includes(kw) ||
      (lop.phongHoc || '').toLowerCase().includes(kw) ||
      (lop.tenGiangVien || '').toLowerCase().includes(kw)
    )
  })

  useEffect(() => {
    hocKyApi.getAll().then((res) => {
      setDanhSachHocKy(res.data)
      if (res.data.length > 0) {
        setMaHocKy(res.data[0].maHocKy)
      }
    })

    boMonApi.getAll().then((res) => {
      setDanhSachBoMon(res.data)
    })
  }, [])

  const taiDanhSachLop = async (hkId: number, bmId?: number) => {
    try {
      setDangTai(true)
      const res = await thoiKhoaBieuApi.getAll(hkId, bmId)
      setDanhSachLop(res.data)
    } catch {
      // ignore
    } finally {
      setDangTai(false)
    }
  }

  useEffect(() => {
    if (maHocKy) {
      taiDanhSachLop(maHocKy, maBoMon)
    }
  }, [maHocKy, maBoMon])

  const handleXoa = async (id: number) => {
    if (!confirm('Bạn có chắc chắn muốn xóa lớp học phần này?')) return
    try {
      await thoiKhoaBieuApi.delete(id)
      taiDanhSachLop(maHocKy, maBoMon)
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: unknown } }
      const data = errorObj.response?.data
      const msg = typeof data === 'string' ? data : (data && typeof data === 'object' && 'message' in data ? String((data as { message: unknown }).message) : 'Không thể xóa lớp học phần này.')
      alert(msg)
    }
  }

  return (
    <section>
      <div className="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
        <div>
          <h2 className="h5 mb-1">Thời khóa biểu lớp học phần</h2>
          <p className="text-muted small mb-0">
            {isAdmin
              ? 'Quản lý các lớp học phần và lịch học cố định theo từng bộ môn và học kỳ'
              : 'Xem lịch học của các lớp học phần theo từng học kỳ'}
          </p>
        </div>

        <div className="d-flex align-items-center gap-2 flex-wrap">
          <label className="form-label small fw-semibold mb-0">Bộ môn:</label>
          <select
            className="form-select form-select-sm"
            style={{ width: '190px' }}
            value={maBoMon}
            onChange={(e) => setMaBoMon(Number(e.target.value))}
          >
            <option value={0}>-- Tất cả bộ môn --</option>
            {danhSachBoMon.map((bm) => (
              <option key={bm.maBoMon} value={bm.maBoMon}>
                {bm.tenBoMon}
              </option>
            ))}
          </select>

          <label className="form-label small fw-semibold mb-0 ms-2">Học kỳ:</label>
          <select
            className="form-select form-select-sm"
            style={{ width: '160px' }}
            value={maHocKy}
            onChange={(e) => setMaHocKy(Number(e.target.value))}
          >
            {danhSachHocKy.map((hk) => (
              <option key={hk.maHocKy} value={hk.maHocKy}>
                {hk.tenHocKy}
              </option>
            ))}
          </select>
        </div>
      </div>

      {isAdmin && (
        <ul className="nav nav-tabs mb-3 no-print">
          <li className="nav-item">
            <button
              className={`nav-link ${tabHienTai === 'danhSach' ? 'active fw-semibold' : ''}`}
              onClick={() => setTabHienTai('danhSach')}
            >
              Danh sách lớp học phần ({danhSachLop.length})
            </button>
          </li>
          <li className="nav-item">
            <button
              className={`nav-link ${tabHienTai === 'import' ? 'active fw-semibold' : ''}`}
              onClick={() => setTabHienTai('import')}
            >
              Import từ Excel theo Bộ môn
            </button>
          </li>
        </ul>
      )}

      {(!isAdmin || tabHienTai === 'danhSach') ? (
        <>
          <div className="d-flex justify-content-between align-items-center mb-2">
            <span className="small text-muted">
              Hiển thị <strong>{dsLopHienThi.length}</strong> / {danhSachLop.length} lớp học phần
            </span>
            <input
              type="text"
              className="form-control form-control-sm"
              style={{ maxWidth: '300px' }}
              placeholder="Tìm theo mã lớp, môn học, giảng viên, phòng..."
              value={tuKhoa}
              onChange={(e) => setTuKhoa(e.target.value)}
            />
          </div>

          <div className="table-responsive border bg-white shadow-sm">
            <table className="table table-hover table-sm mb-0" style={{ fontSize: '0.88rem' }}>
              <thead className="table-light">
                <tr>
                  <th style={{ width: '45px' }}>STT</th>
                  <th>Mã lớp HP</th>
                  <th>Tên lớp</th>
                  <th>Môn học</th>
                  <th>Bộ môn</th>
                  <th>Số TC</th>
                  <th>Thứ</th>
                  <th>Tiết</th>
                  <th>Phòng</th>
                  <th>Tuần</th>
                  <th>Giảng viên</th>
                  {isAdmin && <th style={{ width: '75px' }} className="text-end no-print">Thao tác</th>}
                </tr>
              </thead>
              <tbody>
                {dangTai ? (
                  <tr>
                    <td colSpan={isAdmin ? 12 : 11} className="text-center py-3 text-muted">
                      Đang tải dữ liệu...
                    </td>
                  </tr>
                ) : dsLopHienThi.length === 0 ? (
                  <tr>
                    <td colSpan={isAdmin ? 12 : 11} className="text-center py-3 text-muted">
                      {tuKhoa ? 'Không tìm thấy lớp học phần phù hợp với từ khóa.' : 'Chưa có lớp học phần nào. ' + (isAdmin ? 'Vui lòng qua tab "Import từ Excel theo Bộ môn" để nạp dữ liệu.' : '')}
                    </td>
                  </tr>
                ) : (
                  dsLopHienThi.map((lop, idx) => (
                    <tr
                      key={`${lop.maLopHocPhan}_${lop.thu}_${lop.tietBatDau}_${idx}`}
                      className={lop.trangThaiPhanCong === 'DaKhoa' ? 'table-warning' : ''}
                    >
                      <td>{idx + 1}</td>
                      <td className="fw-semibold">{lop.maLopHocPhanTruong}</td>
                      <td>{lop.tenLop}</td>
                      <td>{lop.tenHocPhan}</td>
                      <td>
                        <span className="badge bg-light text-primary border">
                          {lop.tenBoMon || 'Chưa gán'}
                        </span>
                      </td>
                      <td>{lop.soTinChi}</td>
                      <td>Thứ {lop.thu === 8 ? 'CN' : lop.thu}</td>
                      <td>{lop.tietBatDau} - {lop.tietKetThuc}</td>
                      <td>{lop.phongHoc || '-'}</td>
                      <td>{lop.tuTuan} - {lop.denTuan}</td>
                      <td>
                        {lop.tenGiangVien ? (
                          lop.trangThaiPhanCong === 'DaKhoa' ? (
                            <span className="badge bg-warning text-dark border border-dark-subtle">
                              🔒 {lop.tenGiangVien} (Đã khóa)
                            </span>
                          ) : (
                            <span className="badge bg-info text-dark">
                              {lop.tenGiangVien}
                            </span>
                          )
                        ) : (
                          <span className="text-muted small">Chưa xếp</span>
                        )}
                      </td>
                      {isAdmin && (
                        <td className="text-end no-print">
                          <button
                            className="btn btn-outline-danger btn-sm py-0 px-2"
                            onClick={() => handleXoa(lop.maLopHocPhan)}
                          >
                            Xóa
                          </button>
                        </td>
                      )}
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </>
      ) : (
        <ImportThoiKhoaBieu
          maHocKyChon={maHocKy}
          onImportThanhCong={() => taiDanhSachLop(maHocKy, maBoMon)}
        />
      )}
    </section>
  )
}

export default DanhSachLopHocPhan
