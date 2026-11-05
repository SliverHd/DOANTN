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
  const maBoMonUser = user?.maBoMon

  const [danhSachHocKy, setDanhSachHocKy] = useState<HocKy[]>([])
  const [danhSachBoMon, setDanhSachBoMon] = useState<BoMon[]>([])
  const [maHocKy, setMaHocKy] = useState<number>(0)
  const [maBoMon, setMaBoMon] = useState<number>(!isAdmin && maBoMonUser ? maBoMonUser : 0)
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
      (lop.phongHoc || '').toLowerCase().includes(kw)
    )
  })

  useEffect(() => {
    Promise.all([
      hocKyApi.getAll(),
      isAdmin ? boMonApi.getAll().catch(() => ({ data: [] as BoMon[] })) : Promise.resolve({ data: [] as BoMon[] }),
    ]).then(([resHk, resBm]) => {
      setDanhSachHocKy(resHk.data)
      if (resHk.data.length > 0) {
        setMaHocKy(resHk.data[0].maHocKy)
      }
      if (resBm.data) {
        setDanhSachBoMon(resBm.data)
      }
    })
  }, [isAdmin])

  const taiDanhSachLop = async (hkId: number, bmId?: number) => {
    try {
      setDangTai(true)
      const res = await thoiKhoaBieuApi.getAll(hkId, bmId && bmId > 0 ? bmId : undefined)
      setDanhSachLop(res.data)
    } catch {
      // ignore
    } finally {
      setDangTai(false)
    }
  }

  useEffect(() => {
    if (maHocKy) {
      const bmLoc = !isAdmin && maBoMonUser ? maBoMonUser : (maBoMon > 0 ? maBoMon : undefined)
      taiDanhSachLop(maHocKy, bmLoc)
    }
  }, [maHocKy, maBoMon, isAdmin, maBoMonUser])

  const handleXoa = async (id: number) => {
    if (!confirm('Bạn có chắc chắn muốn xóa lớp học phần này?')) return
    try {
      await thoiKhoaBieuApi.delete(id)
      const bmLoc = !isAdmin && maBoMonUser ? maBoMonUser : (maBoMon > 0 ? maBoMon : undefined)
      taiDanhSachLop(maHocKy, bmLoc)
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: unknown } }
      const data = errorObj.response?.data
      const msg = typeof data === 'string' ? data : (data && typeof data === 'object' && 'message' in data ? String((data as { message: unknown }).message) : 'Không thể xóa lớp học phần này.')
      alert(msg)
    }
  }

  return (
    <section>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 className="h5 mb-1">Thời khóa biểu lớp học phần</h2>
          <p className="text-muted small mb-0">
            {isAdmin
              ? 'Quản lý các lớp học phần và lịch học cố định theo từng học kỳ'
              : 'Xem lịch học của các lớp học phần theo từng học kỳ'}
          </p>
        </div>

        <div className="d-flex align-items-center gap-2">
          {isAdmin ? (
            <>
              <label className="form-label small fw-semibold mb-0">Bộ môn:</label>
              <select
                className="form-select form-select-sm"
                style={{ width: '170px' }}
                value={maBoMon}
                onChange={(e) => setMaBoMon(Number(e.target.value))}
              >
                <option value={0}>-- Tất cả Bộ môn --</option>
                {danhSachBoMon.map((bm) => (
                  <option key={bm.maBoMon} value={bm.maBoMon}>
                    {bm.tenBoMon}
                  </option>
                ))}
              </select>
            </>
          ) : (
            <span className="badge bg-light text-primary border py-2 px-2" style={{ fontSize: '12px' }}>
              Bộ môn: <strong>{user?.tenBoMon || 'Chưa gán'}</strong>
            </span>
          )}

          <label className="form-label small fw-semibold mb-0 ms-2">Học kỳ:</label>
          <select
            className="form-select form-select-sm"
            style={{ width: '180px' }}
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
              Import từ Excel
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
              style={{ maxWidth: '280px' }}
              placeholder="Tìm theo mã lớp, môn học, phòng..."
              value={tuKhoa}
              onChange={(e) => setTuKhoa(e.target.value)}
            />
          </div>

          <div className="table-responsive border bg-white">
            <table className="table table-hover table-sm mb-0" style={{ fontSize: '0.9rem' }}>
              <thead className="table-light">
                <tr>
                  <th style={{ width: '50px' }}>STT</th>
                  <th>Mã lớp HP</th>
                  <th>Tên lớp</th>
                  <th>Môn học</th>
                  <th>Số TC</th>
                  <th>Sĩ số</th>
                  <th>Thứ</th>
                  <th>Tiết</th>
                  <th>Phòng</th>
                  <th>Tuần</th>
                  {isAdmin && <th style={{ width: '80px' }} className="text-end no-print">Thao tác</th>}
                </tr>
              </thead>
              <tbody>
                {dangTai ? (
                  <tr>
                    <td colSpan={isAdmin ? 11 : 10} className="text-center py-3 text-muted">
                      Đang tải dữ liệu...
                    </td>
                  </tr>
                ) : dsLopHienThi.length === 0 ? (
                  <tr>
                    <td colSpan={isAdmin ? 11 : 10} className="text-center py-3 text-muted">
                      {tuKhoa ? 'Không tìm thấy lớp học phần phù hợp với từ khóa.' : 'Học kỳ này chưa có lớp học phần nào. ' + (isAdmin ? 'Vui lòng qua tab "Import từ Excel" để nạp dữ liệu.' : '')}
                    </td>
                  </tr>
                ) : (
                  dsLopHienThi.map((lop, idx) => (
                  <tr key={`${lop.maLopHocPhan}_${lop.thu}_${lop.tietBatDau}_${idx}`}>
                    <td>{idx + 1}</td>

                    <td className="fw-semibold">{lop.maLopHocPhanTruong}</td>
                    <td>{lop.tenLop}</td>
                    <td>{lop.tenHocPhan}</td>
                    <td>{lop.soTinChi}</td>
                    <td>{lop.soLuongSinhVien}</td>
                    <td>Thứ {lop.thu === 8 ? 'CN' : lop.thu}</td>
                    <td>{lop.tietBatDau} - {lop.tietKetThuc}</td>
                    <td>{lop.phongHoc || '-'}</td>
                    <td>{lop.tuTuan} - {lop.denTuan}</td>
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
          maBoMonChon={maBoMon > 0 ? maBoMon : (user?.maBoMon || 0)}
          onImportThanhCong={() => taiDanhSachLop(maHocKy, maBoMon > 0 ? maBoMon : user?.maBoMon)}
        />
      )}
    </section>
  )
}

export default DanhSachLopHocPhan
