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
  maBoMonChon?: number
  onImportThanhCong?: () => void
}

function ImportThoiKhoaBieu({ maHocKyChon, maBoMonChon, onImportThanhCong }: Props) {
  const { user } = useAuth()
  const boMonKhoiTao = maBoMonChon || user?.maBoMon || 0

  const [danhSachHocKy, setDanhSachHocKy] = useState<HocKy[]>([])
  const [maHocKy, setMaHocKy] = useState<number>(maHocKyChon || 0)
  const [danhSachBoMon, setDanhSachBoMon] = useState<BoMon[]>([])
  const [maBoMon, setMaBoMon] = useState<number>(boMonKhoiTao)
  const [file, setFile] = useState<File | null>(null)

  const [dangPreview, setDangPreview] = useState(false)
  const [dangImport, setDangImport] = useState(false)
  const [dangTaiMau, setDangTaiMau] = useState(false)
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
      if (!boMonKhoiTao && user?.maBoMon) {
        setMaBoMon(user.maBoMon)
      }
    })
  }, [])

  useEffect(() => {
    if (maBoMonChon && maBoMonChon > 0) {
      setMaBoMon(maBoMonChon)
    }
  }, [maBoMonChon])

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

  const handleXoaLichSu = async (id: number) => {
    if (!confirm('Bạn có chắc chắn muốn xóa bản ghi lịch sử import này?')) return
    try {
      await importTkbApi.xoaLichSu(id)
      if (maHocKy) {
        taiLichSu(maHocKy)
      }
    } catch {
      alert('Không thể xóa bản ghi lịch sử này.')
    }
  }

  const handleTaiFileMau = async () => {
    try {
      setDangTaiMau(true)
      setLoi('')
      const res = await importTkbApi.taiFileMau()
      const blob = new Blob([res.data], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      })
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = 'Mau_ThoiKhoaBieu.xlsx'
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch {
      setLoi('Không thể tải file mẫu Excel. Vui lòng thử lại.')
    } finally {
      setDangTaiMau(false)
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
      alert('Vui lòng chọn bộ môn.')
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
    if (!maBoMon || maBoMon <= 0) {
      alert('Vui lòng chọn Bộ môn phụ trách để import đúng dữ liệu.')
      return
    }

    if (!ketQuaPreview || ketQuaPreview.soDongHopLe === 0) {
      alert('Không có dòng nào hợp lệ để import.')
      return
    }

    try {
      setDangImport(true)
      setLoi('')
      const soLopKhoa = ketQuaPreview.soLopDaPhanCong || 0
      const res = await importTkbApi.xacNhan(
        maHocKy,
        file ? file.name : 'thoi_khoa_bieu.xlsx',
        ketQuaPreview.chiTiet,
        maBoMon > 0 ? maBoMon : undefined
      )
      const tenBm = danhSachBoMon.find((b) => b.maBoMon === maBoMon)?.tenBoMon || ''
      const msgBm = tenBm ? ` cho ${tenBm}` : ''
      const msgKhoa = soLopKhoa > 0 ? ` và tự động khóa cứng ${soLopKhoa} lớp đã có sẵn giảng viên` : ''
      setThongBao(`Import thành công ${res.data.soDongThanhCong} dòng thời khóa biểu${msgBm}${msgKhoa}!`)
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

  const tenBoMonHienTai =
    danhSachBoMon.find((b) => b.maBoMon === maBoMon)?.tenBoMon || ketQuaPreview?.tenBoMon || ''

  return (
    <div className="card border p-3 bg-white mt-3">
      <div className="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
        <div>
          <h3 className="h6 mb-0 fw-bold">Import thời khóa biểu từ Excel theo Bộ môn</h3>
          <p className="text-muted small mb-0">
            Mỗi bộ môn có thời khóa biểu riêng. Các lớp đã có sẵn tên giảng viên trong file Excel sẽ được <strong>nhận diện và khóa cứng tự động</strong>.
          </p>
        </div>
        <button
          type="button"
          onClick={handleTaiFileMau}
          disabled={dangTaiMau}
          className="btn btn-outline-secondary btn-sm"
        >
          {dangTaiMau ? 'Đang tải mẫu...' : 'Tải file mẫu Excel (.xlsx)'}
        </button>
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
          <label className="form-label small fw-semibold">Bộ môn phụ trách</label>
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
        </div>

        <div className="col-md-4">
          <label className="form-label small fw-semibold">File Excel (.xlsx)</label>
          <input
            type="file"
            className="form-control form-control-sm"
            accept=".xlsx, .xls"
            onChange={handleFileChange}
          />
          <small className="text-muted d-block mt-1" style={{ fontSize: '0.75rem' }}>
            * Lưu ý: Hệ thống xử lý chuẩn file <strong>.xlsx</strong>. Nếu là file <code>.xls</code> từ trường, bạn mở file trong Excel chọn <em>Save As &rarr; Excel Workbook (.xlsx)</em> nhé.
          </small>
        </div>

        <div className="col-md-2 d-flex gap-2">
          <button
            className="btn btn-primary btn-sm flex-grow-1"
            onClick={handlePreview}
            disabled={dangPreview || !file}
          >
            {dangPreview ? 'Đang đọc file...' : 'Xem trước'}
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
                Tổng số dòng: <span className="badge bg-secondary">{ketQuaPreview.tongSoDong}</span>
              </span>
              <span className="fw-semibold small me-3 text-success">
                Hợp lệ: <span className="badge bg-success">{ketQuaPreview.soDongHopLe}</span>
              </span>
              <span className="fw-semibold small me-3 text-warning-emphasis">
                Đã có GV (Khóa cứng):{' '}
                <span className="badge bg-warning text-dark">
                  {ketQuaPreview.soLopDaPhanCong || 0} lớp ({ketQuaPreview.soDongDaPhanCong || 0} buổi)
                </span>
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
              {dangImport
                ? 'Đang lưu dữ liệu...'
                : `Xác nhận import (${ketQuaPreview.soDongHopLe} dòng${tenBoMonHienTai ? ` vào ${tenBoMonHienTai}` : ''})`}
            </button>
          </div>

          <div className="table-responsive border" style={{ maxHeight: '380px' }}>
            <table className="table table-sm table-hover mb-0" style={{ fontSize: '0.85rem' }}>
              <thead className="table-light sticky-top">
                <tr>
                  <th>STT</th>
                  <th>Mã lớp HP</th>
                  <th>Tên lớp</th>
                  <th>Môn học</th>
                  <th>Số TC</th>
                  <th>Thứ</th>
                  <th>Tiết</th>
                  <th>Phòng</th>
                  <th>Tuần</th>
                  <th>Giảng viên (Trong file)</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {ketQuaPreview.chiTiet.map((dong) => (
                  <tr
                    key={dong.soThuTu}
                    className={!dong.hopLe ? 'table-danger' : dong.daPhanCong ? 'table-warning' : ''}
                  >
                    <td>{dong.soThuTu}</td>
                    <td className="fw-semibold">{dong.maLopHocPhanTruong}</td>
                    <td>{dong.tenLop}</td>
                    <td>{dong.tenHocPhan || dong.maHocPhanTruong}</td>
                    <td>{dong.soTinChi || 3}</td>
                    <td>Thứ {dong.thu === 8 ? 'CN' : dong.thu}</td>
                    <td>{dong.tietBatDau} - {dong.tietKetThuc}</td>
                    <td>{dong.phongHoc || '-'}</td>
                    <td>{dong.tuTuan} - {dong.denTuan}</td>
                    <td>
                      {dong.daPhanCong && dong.tenGiangVien ? (
                        <span className="badge bg-warning text-dark border border-dark-subtle">
                          🔒 {dong.tenGiangVien} (Khóa cứng)
                        </span>
                      ) : (
                        <span className="text-muted small">Chờ xếp tự động</span>
                      )}
                    </td>
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
                  <th style={{ width: '80px' }} className="text-center">Thao tác</th>
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
                    <td className="text-center">
                      <button
                        className="btn btn-outline-danger btn-sm py-0 px-2"
                        title="Xóa lịch sử"
                        onClick={() => handleXoaLichSu(ls.maLichSu)}
                      >
                        Xóa
                      </button>
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
