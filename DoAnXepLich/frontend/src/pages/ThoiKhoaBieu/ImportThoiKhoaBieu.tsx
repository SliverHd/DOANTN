import { useState, useEffect, type ChangeEvent } from 'react'
import { hocKyApi } from '../../api/hocKyApi'
import { boMonApi } from '../../api/boMonApi'
import { importTkbApi } from '../../api/importTkbApi'
import { useAuth } from '../../contexts/AuthContext'
import type { HocKy } from '../../types/hocKy'
import type { BoMon } from '../../types/boMon'
import type { KetQuaPreviewImport, LichSuImport } from '../../types/importTkb'

interface Props {
  maHocKyChon?: number
  onImportThanhCong?: () => void
}

function ImportThoiKhoaBieu({ maHocKyChon, onImportThanhCong }: Props) {
  const { user } = useAuth()
  const [danhSachHocKy, setDanhSachHocKy] = useState<HocKy[]>([])
  const [danhSachBoMon, setDanhSachBoMon] = useState<BoMon[]>([])
  const [maHocKy, setMaHocKy] = useState<number>(maHocKyChon || 0)
  const [maBoMon, setMaBoMon] = useState<number>(user?.maBoMon || 0)
  const [file, setFile] = useState<File | null>(null)

  const [dangPreview, setDangPreview] = useState(false)
  const [dangImport, setDangImport] = useState(false)
  const [ketQuaPreview, setKetQuaPreview] = useState<KetQuaPreviewImport | null>(null)
  const [lichSu, setLichSu] = useState<LichSuImport[]>([])
  const [thongBao, setThongBao] = useState('')
  const [loi, setLoi] = useState('')

  useEffect(() => {
    hocKyApi.getAll().then((res) => {
      setDanhSachHocKy(res.data)
      if (!maHocKy && res.data.length > 0) {
        setMaHocKy(res.data[0].maHocKy)
      }
    })

    boMonApi.getAll().then((res) => {
      setDanhSachBoMon(res.data)
      if (!maBoMon) {
        if (user?.maBoMon) {
          setMaBoMon(user.maBoMon)
        } else if (res.data.length > 0) {
          // Mặc định chọn bộ môn đầu tiên nếu có
          setMaBoMon(res.data[0].maBoMon)
        }
      }
    })
  }, [])

  useEffect(() => {
    if (maHocKy) {
      taiLichSu(maHocKy)
    }
  }, [maHocKy])

  const taiLichSu = async (hkId: number) => {
    try {
      const res = await importTkbApi.getLichSu(hkId)
      setLichSu(res.data)
    } catch {
      // bỏ qua lỗi nếu chưa có lịch sử
    }
  }

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0])
      setKetQuaPreview(null)
      setThongBao('')
      setLoi('')
    }
  }

  const handlePreview = async () => {
    if (!maHocKy) {
      alert('Vui lòng chọn học kỳ.')
      return
    }
    if (!maBoMon) {
      alert('Vui lòng chọn bộ môn để import thời khóa biểu.')
      return
    }
    if (!file) {
      alert('Vui lòng chọn file Excel (.xlsx).')
      return
    }

    try {
      setDangPreview(true)
      setLoi('')
      setThongBao('')
      const res = await importTkbApi.preview(file, maHocKy, maBoMon)
      setKetQuaPreview(res.data)
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: unknown } }
      const data = errorObj.response?.data
      if (typeof data === 'string') {
        setLoi(data)
      } else if (data && typeof data === 'object' && 'message' in data) {
        setLoi(String((data as { message: unknown }).message))
      } else {
        setLoi('Không thể đọc file Excel. Vui lòng kiểm tra lại định dạng.')
      }
    } finally {
      setDangPreview(false)
    }
  }

  const handleXacNhanImport = async () => {
    if (!ketQuaPreview || ketQuaPreview.soDongHopLe === 0) {
      alert('Không có dòng nào hợp lệ để import.')
      return
    }
    if (!maBoMon) {
      alert('Vui lòng chọn bộ môn để gán thời khóa biểu.')
      return
    }

    try {
      setDangImport(true)
      setLoi('')
      const res = await importTkbApi.xacNhan(
        maHocKy,
        file ? file.name : 'thoi_khoa_bieu.xlsx',
        ketQuaPreview.chiTiet,
        maBoMon
      )
      const tenBm = danhSachBoMon.find((b) => b.maBoMon === maBoMon)?.tenBoMon || ''
      setThongBao(`Import thành công ${res.data.soDongThanhCong} dòng thời khóa biểu cho ${tenBm}!`)
      setKetQuaPreview(null)
      setFile(null)
      taiLichSu(maHocKy)
      if (onImportThanhCong) onImportThanhCong()
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: unknown } }
      const data = errorObj.response?.data
      if (typeof data === 'string') {
        setLoi(data)
      } else if (data && typeof data === 'object' && 'message' in data) {
        setLoi(String((data as { message: unknown }).message))
      } else {
        setLoi('Lỗi trong quá trình lưu dữ liệu import.')
      }
    } finally {
      setDangImport(false)
    }
  }

  const tenBoMonHienTai = danhSachBoMon.find((b) => b.maBoMon === maBoMon)?.tenBoMon || ''

  return (
    <div className="card border p-3 bg-white mt-3 shadow-sm">
      <div className="mb-3">
        <h3 className="h6 mb-0 fw-bold text-primary">Import thời khóa biểu theo Bộ môn</h3>
        <p className="text-muted small mb-0">
          Mỗi bộ môn có thời khóa biểu riêng. Vui lòng chọn đúng Học kỳ và Bộ môn trước khi nạp file.
        </p>
      </div>

      {loi && <div className="alert alert-danger py-2 small">{loi}</div>}
      {thongBao && <div className="alert alert-success py-2 small">{thongBao}</div>}

      <div className="row g-2 align-items-end mb-3">
        <div className="col-md-3">
          <label className="form-label small fw-semibold">Học kỳ áp dụng</label>
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
          <label className="form-label small fw-semibold">Bộ môn xếp lịch</label>
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
          {user?.vaiTro === 'TruongBoMon' && user?.maBoMon && (
            <small className="text-muted d-block mt-1" style={{ fontSize: '0.72rem' }}>
              * Bộ môn của bạn: <strong>{danhSachBoMon.find(b => b.maBoMon === user.maBoMon)?.tenBoMon}</strong>
            </small>
          )}
        </div>

        <div className="col-md-4">
          <label className="form-label small fw-semibold">File Excel thời khóa biểu (.xlsx)</label>
          <input
            type="file"
            className="form-control form-control-sm"
            accept=".xlsx, .xls"
            onChange={handleFileChange}
          />
          <small className="text-muted d-block mt-1" style={{ fontSize: '0.72rem' }}>
            * Định dạng chuẩn <strong>.xlsx</strong>. Nếu là <code>.xls</code>, mở trong Excel chọn Save As &rarr; Excel Workbook (.xlsx).
          </small>
        </div>

        <div className="col-md-2 d-flex gap-2">
          <button
            className="btn btn-primary btn-sm flex-grow-1"
            onClick={handlePreview}
            disabled={dangPreview || !file}
          >
            {dangPreview ? 'Đang đọc...' : 'Xem trước'}
          </button>
        </div>
      </div>

      {/* Kết quả Preview */}
      {ketQuaPreview && (
        <div className="border-top pt-3 mt-2">
          <div className="d-flex justify-content-between align-items-center mb-2 flex-wrap gap-2">
            <div>
              <span className="badge bg-primary me-2">Bộ môn: {tenBoMonHienTai || 'Chưa chọn'}</span>
              <span className="fw-semibold small me-3">
                Tổng dòng: <span className="badge bg-secondary">{ketQuaPreview.tongSoDong}</span>
              </span>
              <span className="fw-semibold small me-3 text-success">
                Hợp lệ: <span className="badge bg-success">{ketQuaPreview.soDongHopLe}</span>
              </span>
              <span className="fw-semibold small text-danger">
                Lỗi: <span className="badge bg-danger">{ketQuaPreview.soDongLoi}</span>
              </span>
            </div>

            <button
              className="btn btn-success btn-sm"
              onClick={handleXacNhanImport}
              disabled={dangImport || ketQuaPreview.soDongHopLe === 0}
            >
              {dangImport ? 'Đang lưu dữ liệu...' : `Xác nhận import (${ketQuaPreview.soDongHopLe} dòng vào ${tenBoMonHienTai})`}
            </button>
          </div>

          <div className="table-responsive border" style={{ maxHeight: '350px' }}>
            <table className="table table-sm table-hover mb-0" style={{ fontSize: '0.85rem' }}>
              <thead className="table-light sticky-top">
                <tr>
                  <th>STT</th>
                  <th>Mã lớp HP</th>
                  <th>Tên lớp</th>
                  <th>Môn học</th>
                  <th>Thứ</th>
                  <th>Tiết</th>
                  <th>Phòng</th>
                  <th>Tuần</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {ketQuaPreview.chiTiet.map((dong) => (
                  <tr key={dong.soThuTu} className={dong.hopLe ? '' : 'table-danger'}>
                    <td>{dong.soThuTu}</td>
                    <td className="fw-semibold">{dong.maLopHocPhanTruong}</td>
                    <td>{dong.tenLop}</td>
                    <td>{dong.tenHocPhan || dong.maHocPhanTruong}</td>
                    <td>Thứ {dong.thu === 8 ? 'CN' : dong.thu}</td>
                    <td>{dong.tietBatDau} - {dong.tietKetThuc}</td>
                    <td>{dong.phongHoc || '-'}</td>
                    <td>{dong.tuTuan} - {dong.denTuan}</td>
                    <td>
                      {dong.hopLe ? (
                        <span className="badge bg-success">Hợp lệ</span>
                      ) : (
                        <span className="text-danger small" title={dong.danhSachLoi.join('; ')}>
                          {dong.danhSachLoi[0]}
                        </span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Lịch sử import */}
      <div className="border-top pt-3 mt-4">
        <h4 className="h6 fw-bold mb-2">Lịch sử import gần đây</h4>
        {lichSu.length === 0 ? (
          <p className="text-muted small mb-0">Chưa có lịch sử import cho học kỳ này.</p>
        ) : (
          <div className="table-responsive border">
            <table className="table table-sm table-hover mb-0" style={{ fontSize: '0.85rem' }}>
              <thead className="table-light">
                <tr>
                  <th>Thời gian</th>
                  <th>Tên file</th>
                  <th>Ghi chú / Bộ môn</th>
                  <th>Tổng dòng</th>
                  <th>Thành công</th>
                  <th>Lỗi</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {lichSu.map((ls) => (
                  <tr key={ls.maLichSu}>
                    <td>{new Date(ls.ngayImport).toLocaleString('vi-VN')}</td>
                    <td className="fw-semibold">{ls.tenFile}</td>
                    <td><small className="text-muted">{ls.ghiChu || '-'}</small></td>
                    <td>{ls.tongSoDong}</td>
                    <td className="text-success fw-semibold">{ls.soDongThanhCong}</td>
                    <td className={ls.soDongLoi > 0 ? 'text-danger fw-semibold' : ''}>
                      {ls.soDongLoi}
                    </td>
                    <td>
                      <span
                        className={`badge ${
                          ls.trangThai === 'ThanhCong'
                            ? 'bg-success'
                            : ls.trangThai === 'MotPhan'
                            ? 'bg-warning text-dark'
                            : 'bg-danger'
                        }`}
                      >
                        {ls.trangThai === 'ThanhCong' ? 'Thành công' : ls.trangThai === 'MotPhan' ? 'Một phần' : 'Thất bại'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

export default ImportThoiKhoaBieu
