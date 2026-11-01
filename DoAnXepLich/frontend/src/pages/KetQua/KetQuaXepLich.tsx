import { useEffect, useState } from 'react'
import { hocKyApi } from '../../api/hocKyApi'
import { xepLichApi } from '../../api/xepLichApi'
import type { HocKy } from '../../types/hocKy'
import type {
  BangPhanCongHocKy,
  DongPhanCongLop,
  ThongKeTaiGv,
  YeuCauPhanCongThuCong,
} from '../../types/xepLich'

interface CanhBaoModalState {
  hienThi: boolean
  maLopHocPhan: number
  maGiangVien: number
  tenLop: string
  tenGiangVien: string
  danhSachViPham: string[]
}

const CAC_THU_MA_TRAN = [
  { id: 2, ten: 'Thứ 2' },
  { id: 3, ten: 'Thứ 3' },
  { id: 4, ten: 'Thứ 4' },
  { id: 5, ten: 'Thứ 5' },
  { id: 6, ten: 'Thứ 6' },
  { id: 7, ten: 'Thứ 7' },
  { id: 8, ten: 'Chủ nhật' },
]

const CAC_TIET_MA_TRAN = Array.from({ length: 12 }, (_, i) => i + 1)

function KetQuaXepLich() {
  const [danhSachHocKy, setDanhSachHocKy] = useState<HocKy[]>([])
  const [maHocKy, setMaHocKy] = useState<number>(0)
  const [bangPhanCong, setBangPhanCong] = useState<BangPhanCongHocKy | null>(null)
  const [thongKeTai, setThongKeTai] = useState<ThongKeTaiGv[]>([])
  const [dangTai, setDangTai] = useState<boolean>(false)
  const [dangXuLy, setDangXuLy] = useState<boolean>(false)
  const [tabHienTai, setTabHienTai] = useState<'phanCong' | 'thongKe' | 'maTran'>('phanCong')
  const [maGvMaTran, setMaGvMaTran] = useState<number>(0)
  const [boLocTrangThai, setBoLocTrangThai] = useState<string>('TatCa')
  const [tuKhoaTimKiem, setTuKhoaTimKiem] = useState<string>('')

  // State cho Modal cảnh báo vi phạm
  const [modalCanhBao, setModalCanhBao] = useState<CanhBaoModalState>({
    hienThi: false,
    maLopHocPhan: 0,
    maGiangVien: 0,
    tenLop: '',
    tenGiangVien: '',
    danhSachViPham: [],
  })

  // 1. Tải danh sách học kỳ
  useEffect(() => {
    hocKyApi.getAll().then((res) => {
      setDanhSachHocKy(res.data)
      if (res.data.length > 0) {
        setMaHocKy(res.data[0].maHocKy)
      }
    })
  }, [])

  // 2. Tải dữ liệu phân công & thống kê theo học kỳ
  const taiDuLieu = async (hkId: number) => {
    if (!hkId) return
    try {
      setDangTai(true)
      const [resBang, resTai] = await Promise.all([
        xepLichApi.layBangPhanCong(hkId),
        xepLichApi.layThongKeTai(hkId),
      ])
      setBangPhanCong(resBang.data)
      setThongKeTai(resTai.data)
    } catch (err) {
      console.error('Lỗi khi tải bảng phân công:', err)
    } finally {
      setDangTai(false)
    }
  }

  useEffect(() => {
    if (maHocKy > 0) {
      taiDuLieu(maHocKy)
    }
  }, [maHocKy])

  // Tự động chọn giảng viên mặc định khi tải xong danh sách thống kê tải
  useEffect(() => {
    if (thongKeTai.length > 0 && maGvMaTran === 0) {
      const gvCoLop = thongKeTai.find((g) => g.soLopPhanCong > 0)
      setMaGvMaTran(gvCoLop ? gvCoLop.maGiangVien : thongKeTai[0].maGiangVien)
    }
  }, [thongKeTai, maGvMaTran])

  // 3. Xử lý phân công thủ công / đổi giảng viên
  const handleChonGiangVien = async (
    lop: DongPhanCongLop,
    maGvMoi: number,
    boQuaCanhBao = false
  ) => {
    if (maGvMoi <= 0) return

    const ungVien = lop.danhSachUngVien.find((u) => u.maGiangVien === maGvMoi)
    const tenGv = ungVien ? ungVien.hoTen : `Giảng viên #${maGvMoi}`

    const req: YeuCauPhanCongThuCong = {
      maLopHocPhan: lop.maLopHocPhan,
      maGiangVien: maGvMoi,
      boQuaCanhBao,
    }

    try {
      setDangXuLy(true)
      const res = await xepLichApi.phanCongThuCong(req)

      if (res.data.canXacNhan && !boQuaCanhBao) {
        // Có vi phạm cần xác nhận
        setModalCanhBao({
          hienThi: true,
          maLopHocPhan: lop.maLopHocPhan,
          maGiangVien: maGvMoi,
          tenLop: `${lop.maLopHocPhanTruong} - ${lop.tenHocPhan}`,
          tenGiangVien: tenGv,
          danhSachViPham: res.data.danhSachViPham,
        })
      } else if (res.data.thanhCong) {
        setModalCanhBao((prev) => ({ ...prev, hienThi: false }))
        await taiDuLieu(maHocKy)
      } else {
        alert(res.data.thongBao || 'Không thể thực hiện phân công.')
      }
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: unknown } }
      const data = errorObj.response?.data
      const msg = typeof data === 'string' ? data : 'Lỗi khi cập nhật phân công.'
      alert(msg)
    } finally {
      setDangXuLy(false)
    }
  }

  // 4. Khóa / Mở khóa phân công
  const handleDoiKhoa = async (lop: DongPhanCongLop) => {
    if (!lop.maGiangVien) {
      alert('Lớp chưa được phân công giảng viên, không thể khóa.')
      return
    }

    const trangThaiMoi = !lop.daKhoa
    try {
      setDangXuLy(true)
      await xepLichApi.doiTrangThaiKhoa(lop.maLopHocPhan, trangThaiMoi)
      await taiDuLieu(maHocKy)
    } catch {
      alert('Không thể đổi trạng thái khóa.')
    } finally {
      setDangXuLy(false)
    }
  }

  // 5. Hủy phân công
  const handleHuyPhanCong = async (lop: DongPhanCongLop) => {
    if (!confirm(`Bạn có chắc chắn muốn hủy phân công lớp [${lop.maLopHocPhanTruong}]?`)) {
      return
    }

    try {
      setDangXuLy(true)
      await xepLichApi.huyPhanCong(lop.maLopHocPhan)
      await taiDuLieu(maHocKy)
    } catch {
      alert('Không thể hủy phân công.')
    } finally {
      setDangXuLy(false)
    }
  }

  // 5b. Đặt lại bảng phân công (Làm trống các lớp chưa khóa)
  const handleDatLaiPhanCong = async () => {
    const xacNhan = window.confirm(
      'Bạn có chắc chắn muốn đặt lại bảng phân công?\n\nToàn bộ các phân công dự kiến (chưa khóa) sẽ bị xóa để đưa bảng về trạng thái trống.\nCác lớp đã Khóa (nếu có) sẽ được giữ nguyên.'
    )
    if (!xacNhan) return

    try {
      setDangXuLy(true)
      const res = await xepLichApi.datLaiPhanCong(maHocKy)
      alert(res.data.thongBao || 'Đã đặt lại bảng phân công thành công.')
      await taiDuLieu(maHocKy)
    } catch {
      alert('Lỗi khi đặt lại bảng phân công.')
    } finally {
      setDangXuLy(false)
    }
  }

  // 6. Xuất báo cáo Excel
  const [dangXuatExcel, setDangXuatExcel] = useState<boolean>(false)

  const handleXuatExcel = async () => {
    if (!maHocKy) return
    try {
      setDangXuatExcel(true)
      const res = await xepLichApi.xuatExcel(maHocKy)
      const blob = new Blob([res.data], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      })
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `ThoiKhoaBieu_PhanCong_HocKy_${maHocKy}.xlsx`
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      window.URL.revokeObjectURL(url)
    } catch {
      alert('Không thể xuất file Excel. Vui lòng thử lại sau.')
    } finally {
      setDangXuatExcel(false)
    }
  }

  // Lọc danh sách lớp học phần hiển thị
  const dsLopHienThi = (bangPhanCong?.danhSachLop || []).filter((lop) => {
    if (boLocTrangThai === 'DaKhoa' && !lop.daKhoa) return false
    if (boLocTrangThai === 'ChuaPhanCong' && lop.maGiangVien) return false
    if (boLocTrangThai === 'DaPhanCong' && !lop.maGiangVien) return false

    if (tuKhoaTimKiem.trim()) {
      const kw = tuKhoaTimKiem.toLowerCase()
      const matchMa = lop.maLopHocPhanTruong.toLowerCase().includes(kw)
      const matchTenHp = lop.tenHocPhan.toLowerCase().includes(kw)
      const matchTenGv = (lop.tenGiangVien || '').toLowerCase().includes(kw)
      const matchPhong = (lop.phongHoc || '').toLowerCase().includes(kw)
      return matchMa || matchTenHp || matchTenGv || matchPhong
    }

    return true
  })

  // Dữ liệu và hàm phục vụ ma trận thời khóa biểu
  const dsLopCuaGv = (bangPhanCong?.danhSachLop || []).filter((lop) => {
    if (maGvMaTran === -1) {
      return !lop.maGiangVien || lop.maGiangVien === 0
    }
    return lop.maGiangVien === maGvMaTran
  })

  const thongTinGvChon = thongKeTai.find((g) => g.maGiangVien === maGvMaTran)

  const renderCellMaTran = (thu: number, tiet: number) => {
    const cacLopThuNay = dsLopCuaGv.filter((lop) => lop.thu === thu)
    const lopChiemCho = cacLopThuNay.find(
      (lop) => tiet > lop.tietBatDau && tiet <= lop.tietKetThuc
    )
    if (lopChiemCho) {
      return null
    }

    const cacLopBatDau = cacLopThuNay.filter((lop) => lop.tietBatDau === tiet)
    if (cacLopBatDau.length === 0) {
      return (
        <td
          key={`${thu}_${tiet}`}
          className="text-center text-muted align-middle bg-white"
          style={{ minWidth: '135px', height: '42px' }}
        >
          <span className="text-black-50 small">-</span>
        </td>
      )
    }

    if (cacLopBatDau.length === 1) {
      const lop = cacLopBatDau[0]
      const span = Math.max(1, lop.tietKetThuc - lop.tietBatDau + 1)
      return (
        <td
          key={`${thu}_${tiet}`}
          rowSpan={span}
          className={`align-top p-2 ${
            lop.daKhoa ? 'table-primary border-primary' : 'table-light border-success'
          }`}
          style={{ minWidth: '135px' }}
        >
          <div className="d-flex flex-column h-100 justify-content-between gap-1">
            <div>
              <div className="d-flex justify-content-between align-items-start mb-1">
                <span
                  className={`badge ${lop.daKhoa ? 'bg-primary' : 'bg-success'}`}
                  style={{ fontSize: '10px' }}
                >
                  {lop.daKhoa ? 'Đã khóa' : 'Dự kiến'}
                </span>
                <span className="badge bg-secondary" style={{ fontSize: '10px' }}>
                  {lop.soTinChi} TC
                </span>
              </div>
              <div className="fw-bold text-dark small lh-sm">{lop.tenHocPhan}</div>
              <div className="text-muted" style={{ fontSize: '11px' }}>
                {lop.maLopHocPhanTruong}
              </div>
              {lop.tenLop && (
                <div className="text-muted" style={{ fontSize: '11px' }}>
                  Lớp: {lop.tenLop}
                </div>
              )}
            </div>
            <div className="border-top pt-1 mt-1 text-muted" style={{ fontSize: '11px' }}>
              <div>
                <strong>Tiết:</strong> {lop.tietBatDau}-{lop.tietKetThuc}
                {lop.phongHoc && ` | P: ${lop.phongHoc}`}
              </div>
              {lop.tuTuan > 0 && (
                <div>
                  <strong>Tuần:</strong> {lop.tuTuan}-{lop.denTuan}
                </div>
              )}
              {maGvMaTran === -1 && <div className="text-danger mt-1">Chưa có GV</div>}
            </div>
          </div>
        </td>
      )
    }

    return (
      <td
        key={`${thu}_${tiet}`}
        className="align-top p-1 table-warning"
        style={{ minWidth: '135px' }}
      >
        <div className="d-flex flex-column gap-1">
          {cacLopBatDau.map((lop) => (
            <div
              key={lop.maLopHocPhan}
              className="border rounded p-1 bg-white shadow-sm small"
            >
              <div className="fw-bold text-dark" style={{ fontSize: '11px' }}>
                {lop.tenHocPhan}
              </div>
              <div className="text-muted" style={{ fontSize: '10px' }}>
                {lop.maLopHocPhanTruong} | Tiết {lop.tietBatDau}-{lop.tietKetThuc}
              </div>
            </div>
          ))}
        </div>
      </td>
    )
  }

  return (
    <section>
      {/* Tiêu đề & Chọn học kỳ */}
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
        <div>
          <h2 className="h4 mb-1">Bảng phân công giảng dạy & Can thiệp bán tự động</h2>
          <div className="text-secondary small">
            Quản lý, can thiệp thủ công, ghim khóa lớp và theo dõi định mức tải giảng viên.
          </div>
        </div>
        <div className="d-flex align-items-center gap-2 mt-2 mt-sm-0">
          <label htmlFor="selectHocKy" className="col-form-label-sm text-nowrap fw-semibold">
            Học kỳ:
          </label>
          <select
            id="selectHocKy"
            className="form-select form-select-sm"
            style={{ width: 'auto', minWidth: '180px' }}
            value={maHocKy}
            onChange={(e) => setMaHocKy(Number(e.target.value))}
            disabled={dangTai || dangXuLy}
          >
            {danhSachHocKy.map((hk) => (
              <option key={hk.maHocKy} value={hk.maHocKy}>
                {hk.tenHocKy}
              </option>
            ))}
          </select>

          <button
            type="button"
            className="btn btn-sm btn-outline-danger"
            onClick={handleDatLaiPhanCong}
            disabled={dangTai || dangXuLy || dangXuatExcel || !bangPhanCong || bangPhanCong.soLopDaPhanCong === 0}
            title="Xóa tất cả các phân công dự kiến để làm trống bảng (các lớp đã khóa sẽ được giữ nguyên)"
          >
            Đặt lại bảng (Làm trống)
          </button>
          <button
            type="button"
            className="btn btn-sm btn-outline-success"
            onClick={handleXuatExcel}
            disabled={dangTai || dangXuLy || dangXuatExcel || !bangPhanCong || bangPhanCong.tongSoLop === 0}
            title="Xuất bảng thời khóa biểu và thống kê tải ra file Excel (.xlsx)"
          >
            {dangXuatExcel ? 'Đang xuất...' : 'Xuất Excel'}
          </button>
        </div>
      </div>

      {/* Thanh số liệu tóm tắt tối giản */}
      {bangPhanCong && (
        <div className="row g-2 mb-3">
          <div className="col-6 col-md-3">
            <div className="border rounded p-2 bg-light text-center">
              <div className="small text-muted">Tổng số lớp</div>
              <div className="fs-5 fw-bold">{bangPhanCong.tongSoLop}</div>
            </div>
          </div>
          <div className="col-6 col-md-3">
            <div className="border rounded p-2 bg-light text-center">
              <div className="small text-muted">Đã phân công</div>
              <div className="fs-5 fw-bold text-success">{bangPhanCong.soLopDaPhanCong}</div>
            </div>
          </div>
          <div className="col-6 col-md-3">
            <div className="border rounded p-2 bg-light text-center">
              <div className="small text-muted">Chưa phân công</div>
              <div className="fs-5 fw-bold text-danger">{bangPhanCong.soLopChuaPhanCong}</div>
            </div>
          </div>
          <div className="col-6 col-md-3">
            <div className="border rounded p-2 bg-light text-center">
              <div className="small text-muted">Đã khóa (Ghim)</div>
              <div className="fs-5 fw-bold text-primary">{bangPhanCong.soLopDaKhoa}</div>
            </div>
          </div>
        </div>
      )}

      {/* Tab điều hướng */}
      <ul className="nav nav-tabs mb-3">
        <li className="nav-item">
          <button
            type="button"
            className={`nav-link ${tabHienTai === 'phanCong' ? 'active fw-semibold' : ''}`}
            onClick={() => setTabHienTai('phanCong')}
          >
            Bảng phân công thời khóa biểu ({bangPhanCong?.tongSoLop || 0})
          </button>
        </li>
        <li className="nav-item">
          <button
            type="button"
            className={`nav-link ${tabHienTai === 'thongKe' ? 'active fw-semibold' : ''}`}
            onClick={() => setTabHienTai('thongKe')}
          >
            Thống kê tải giảng dạy & Định mức ({thongKeTai.length})
          </button>
        </li>
        <li className="nav-item">
          <button
            type="button"
            className={`nav-link ${tabHienTai === 'maTran' ? 'active fw-semibold' : ''}`}
            onClick={() => setTabHienTai('maTran')}
          >
            Ma trận thời khóa biểu (Thứ / Tiết)
          </button>
        </li>
      </ul>

      {/* TAB 1: BẢNG PHÂN CÔNG THỜI KHÓA BIỂU */}
      {tabHienTai === 'phanCong' && (
        <>
          {/* Thanh lọc & Tìm kiếm */}
          <div className="row g-2 mb-3 align-items-center">
            <div className="col-md-4 col-sm-6">
              <input
                type="text"
                className="form-control form-control-sm"
                placeholder="Tìm theo mã lớp, tên môn, giảng viên, phòng..."
                value={tuKhoaTimKiem}
                onChange={(e) => setTuKhoaTimKiem(e.target.value)}
              />
            </div>
            <div className="col-md-3 col-sm-6">
              <select
                className="form-select form-select-sm"
                value={boLocTrangThai}
                onChange={(e) => setBoLocTrangThai(e.target.value)}
              >
                <option value="TatCa">Tất cả trạng thái</option>
                <option value="DaPhanCong">Đã phân công</option>
                <option value="ChuaPhanCong">Chưa phân công</option>
                <option value="DaKhoa">Đã khóa (Ghim)</option>
              </select>
            </div>
            <div className="col text-end text-muted small">
              Hiển thị: <strong>{dsLopHienThi.length}</strong> / {bangPhanCong?.tongSoLop || 0} lớp
            </div>
          </div>

          {dangTai ? (
            <div className="p-4 text-center text-muted border rounded">Đang tải dữ liệu...</div>
          ) : dsLopHienThi.length === 0 ? (
            <div className="p-4 text-center text-muted border rounded">
              Không tìm thấy lớp học phần nào phù hợp trong học kỳ này.
            </div>
          ) : (
            <div className="table-responsive border rounded">
              <table className="table table-bordered table-hover table-sm align-middle mb-0">
                <thead className="table-light">
                  <tr className="text-nowrap text-center">
                    <th style={{ width: '45px' }}>STT</th>
                    <th style={{ width: '110px' }}>Mã lớp HP</th>
                    <th>Học phần</th>
                    <th style={{ width: '50px' }}>TC</th>
                    <th style={{ width: '130px' }}>Lịch học</th>
                    <th style={{ width: '80px' }}>Phòng</th>
                    <th style={{ width: '70px' }}>Tuần</th>
                    <th style={{ minWidth: '240px' }}>Giảng viên phân công</th>
                    <th style={{ width: '110px' }}>Trạng thái</th>
                    <th style={{ width: '130px' }}>Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {dsLopHienThi.map((lop, idx) => {
                    const daPhanCong = lop.maGiangVien && lop.maGiangVien > 0
                    return (
                      <tr
                        key={lop.maLopHocPhan}
                        className={lop.daKhoa ? 'table-secondary' : ''}
                      >
                        <td className="text-center text-muted">{idx + 1}</td>
                        <td className="fw-semibold text-nowrap">{lop.maLopHocPhanTruong}</td>
                        <td>
                          <div className="fw-medium">{lop.tenHocPhan}</div>
                          {lop.tenLop && (
                            <div className="small text-muted">Lớp: {lop.tenLop} ({lop.soLuongSinhVien} SV)</div>
                          )}
                        </td>
                        <td className="text-center">{lop.soTinChi}</td>
                        <td className="text-center text-nowrap">
                          {lop.thu > 0 ? (
                            <span>
                              Thứ {lop.thu}, Tiết {lop.tietBatDau}-{lop.tietKetThuc}
                              <span className="text-muted ms-1">({lop.soTiet}t)</span>
                            </span>
                          ) : (
                            <span className="text-muted">-</span>
                          )}
                        </td>
                        <td className="text-center">{lop.phongHoc || '-'}</td>
                        <td className="text-center text-nowrap">
                          {lop.tuTuan > 0 ? `${lop.tuTuan}-${lop.denTuan}` : '-'}
                        </td>
                        <td>
                          {/* Dropdown chọn / can thiệp giảng viên */}
                          <div className="d-flex flex-column gap-1">
                            <select
                              className={`form-select form-select-sm ${
                                !daPhanCong ? 'border-warning' : ''
                              }`}
                              value={lop.maGiangVien || 0}
                              onChange={(e) =>
                                handleChonGiangVien(lop, Number(e.target.value))
                              }
                              disabled={dangXuLy || lop.daKhoa}
                            >
                              <option value={0}>-- Chưa phân công --</option>
                              {lop.danhSachUngVien.map((uv) => {
                                let nhan = uv.hoTen
                                if (uv.chucDanh) nhan += ` (${uv.chucDanh})`
                                if (uv.hopLe) {
                                  nhan += ' [Hợp lệ]'
                                } else {
                                  nhan += ` [! ${uv.lyDoKhongHopLe || 'Vi phạm'}]`
                                }
                                return (
                                  <option key={uv.maGiangVien} value={uv.maGiangVien}>
                                    {nhan}
                                  </option>
                                )
                              })}
                            </select>

                            {/* Thông tin phụ nguồn phân công */}
                            {daPhanCong && (
                              <div className="small text-muted d-flex gap-2">
                                <span>
                                  Nguồn:{' '}
                                  {lop.nguonPhanCong === 'ThuCong'
                                    ? 'Thủ công'
                                    : lop.nguonPhanCong || 'Chưa rõ'}
                                </span>
                                {lop.emailGiangVien && <span>| {lop.emailGiangVien}</span>}
                              </div>
                            )}

                            {/* Cảnh báo nếu lớp không có ứng viên nào có năng lực */}
                            {lop.danhSachUngVien.length === 0 && (
                              <div className="small text-danger">
                                Chưa có GV nào được phân công năng lực môn này.
                              </div>
                            )}
                          </div>
                        </td>
                        <td className="text-center text-nowrap">
                          {lop.daKhoa ? (
                            <span className="badge bg-primary">Đã khóa</span>
                          ) : daPhanCong ? (
                            <span className="badge bg-success">Dự kiến</span>
                          ) : (
                            <span className="badge bg-secondary">Chưa phân</span>
                          )}
                        </td>
                        <td className="text-center text-nowrap">
                          <div className="btn-group btn-group-sm">
                            {/* Nút Khóa / Mở khóa */}
                            <button
                              type="button"
                              className={`btn ${
                                lop.daKhoa ? 'btn-outline-primary' : 'btn-outline-secondary'
                              }`}
                              title={lop.daKhoa ? 'Mở khóa phân công' : 'Khóa / Ghim phân công'}
                              onClick={() => handleDoiKhoa(lop)}
                              disabled={dangXuLy || !daPhanCong}
                            >
                              {lop.daKhoa ? 'Mở khóa' : 'Khóa'}
                            </button>

                            {/* Nút Hủy phân công */}
                            <button
                              type="button"
                              className="btn btn-outline-danger"
                              title="Hủy phân công về trống"
                              onClick={() => handleHuyPhanCong(lop)}
                              disabled={dangXuLy || !daPhanCong || lop.daKhoa}
                            >
                              Hủy
                            </button>
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}

      {/* TAB 2: THỐNG KÊ TẢI GIẢNG DẠY & ĐỊNH MỨC */}
      {tabHienTai === 'thongKe' && (
        <>
          <div className="alert alert-light border small mb-3">
            <strong>Ghi chú:</strong> Bảng thống kê tổng hợp số lớp, số tín chỉ và tổng số tiết giảng dạy mỗi tuần trong học kỳ của từng giảng viên. Đối chiếu với định mức quy định (Số tiết tối thiểu - tối đa) để cân bằng khối lượng công việc.
          </div>

          {dangTai ? (
            <div className="p-4 text-center text-muted border rounded">Đang tải dữ liệu...</div>
          ) : thongKeTai.length === 0 ? (
            <div className="p-4 text-center text-muted border rounded">
              Chưa có dữ liệu giảng viên trong hệ thống.
            </div>
          ) : (
            <div className="table-responsive border rounded">
              <table className="table table-bordered table-hover table-sm align-middle mb-0">
                <thead className="table-light">
                  <tr className="text-nowrap text-center">
                    <th style={{ width: '45px' }}>STT</th>
                    <th>Họ và tên</th>
                    <th style={{ width: '120px' }}>Chức danh</th>
                    <th style={{ width: '90px' }}>Số lớp</th>
                    <th style={{ width: '90px' }}>Tổng TC</th>
                    <th style={{ width: '110px' }}>Tiết/tuần</th>
                    <th style={{ width: '140px' }}>Định mức (Min - Max)</th>
                    <th style={{ width: '120px' }}>Trạng thái tải</th>
                  </tr>
                </thead>
                <tbody>
                  {thongKeTai.map((gv, idx) => {
                    let badgeClass = 'bg-success'
                    let tenTrangThai = 'Đạt định mức'
                    if (gv.trangThaiTai === 'ThieuTai') {
                      badgeClass = 'bg-warning text-dark'
                      tenTrangThai = 'Thiếu tải'
                    } else if (gv.trangThaiTai === 'QuaTai') {
                      badgeClass = 'bg-danger'
                      tenTrangThai = 'Quá tải'
                    }

                    return (
                      <tr key={gv.maGiangVien}>
                        <td className="text-center text-muted">{idx + 1}</td>
                        <td>
                          <div className="fw-semibold">{gv.hoTen}</div>
                          <div className="small text-muted">{gv.email}</div>
                        </td>
                        <td className="text-center">{gv.chucDanh || '-'}</td>
                        <td className="text-center fw-bold">{gv.soLopPhanCong}</td>
                        <td className="text-center">{gv.tongSoTinChi}</td>
                        <td className="text-center fw-semibold">{gv.tongSoTietTuan}</td>
                        <td className="text-center text-nowrap">
                          {gv.soTietToiThieu > 0 || gv.soTietToiDa > 0
                            ? `${gv.soTietToiThieu} - ${gv.soTietToiDa} tiết`
                            : 'Chưa cài đặt'}
                        </td>
                        <td className="text-center">
                          <span className={`badge ${badgeClass}`}>{tenTrangThai}</span>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}

      {/* TAB 3: MA TRẬN THỜI KHÓA BIỂU THỨ / TIẾT */}
      {tabHienTai === 'maTran' && (
        <>
          {/* Thanh công cụ chọn giảng viên & Thao tác */}
          <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3 bg-light p-2 border rounded">
            <div className="d-flex align-items-center gap-2">
              <label htmlFor="selectGvMaTran" className="col-form-label-sm text-nowrap fw-semibold">
                Xem thời khóa biểu của:
              </label>
              <select
                id="selectGvMaTran"
                className="form-select form-select-sm"
                style={{ width: 'auto', minWidth: '280px' }}
                value={maGvMaTran}
                onChange={(e) => setMaGvMaTran(Number(e.target.value))}
              >
                <optgroup label="Giảng viên trong học kỳ">
                  {thongKeTai.map((gv) => (
                    <option key={gv.maGiangVien} value={gv.maGiangVien}>
                      {gv.hoTen} ({gv.soLopPhanCong} lớp - {gv.tongSoTietTuan} tiết/tuần)
                    </option>
                  ))}
                </optgroup>
                <optgroup label="Tùy chọn khác">
                  <option value={-1}>
                    -- Danh sách lớp chưa phân công ({bangPhanCong?.soLopChuaPhanCong || 0} lớp) --
                  </option>
                </optgroup>
              </select>
            </div>

            <div className="d-flex align-items-center gap-2">
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary"
                onClick={() => window.print()}
                title="In ma trận thời khóa biểu tuần"
              >
                In TKB
              </button>
            </div>
          </div>

          {/* Thẻ tóm tắt thông tin giảng viên đang xem */}
          {thongTinGvChon && maGvMaTran > 0 && (
            <div className="alert alert-light border small py-2 px-3 mb-3 d-flex flex-wrap justify-content-between align-items-center">
              <div>
                <strong>{thongTinGvChon.hoTen}</strong>
                {thongTinGvChon.chucDanh && <span> ({thongTinGvChon.chucDanh})</span>}
                <span className="text-muted ms-2">Email: {thongTinGvChon.email}</span>
              </div>
              <div className="d-flex gap-3">
                <span>Số lớp: <strong>{thongTinGvChon.soLopPhanCong}</strong></span>
                <span>Tổng TC: <strong>{thongTinGvChon.tongSoTinChi}</strong></span>
                <span>Tiết/tuần: <strong>{thongTinGvChon.tongSoTietTuan}</strong></span>
                <span>
                  Định mức:{' '}
                  {thongTinGvChon.soTietToiThieu > 0 || thongTinGvChon.soTietToiDa > 0
                    ? `${thongTinGvChon.soTietToiThieu}-${thongTinGvChon.soTietToiDa} tiết`
                    : 'Chưa cài đặt'}
                </span>
                <span>
                  Trạng thái:{' '}
                  <span
                    className={`badge ${
                      thongTinGvChon.trangThaiTai === 'QuaTai'
                        ? 'bg-danger'
                        : thongTinGvChon.trangThaiTai === 'ThieuTai'
                        ? 'bg-warning text-dark'
                        : 'bg-success'
                    }`}
                  >
                    {thongTinGvChon.trangThaiTai === 'QuaTai'
                      ? 'Quá tải'
                      : thongTinGvChon.trangThaiTai === 'ThieuTai'
                      ? 'Thiếu tải'
                      : 'Đạt định mức'}
                  </span>
                </span>
              </div>
            </div>
          )}

          {maGvMaTran === -1 && (
            <div className="alert alert-warning small py-2 px-3 mb-3">
              Đang hiển thị ma trận phân bổ các <strong>lớp học phần chưa được phân công</strong> trong tuần ({dsLopCuaGv.length} lớp).
            </div>
          )}

          {/* Bảng lưới Ma trận Thứ / Tiết */}
          <div className="table-responsive border rounded bg-white">
            <table className="table table-bordered table-sm mb-0 align-middle">
              <thead className="table-light text-center">
                <tr>
                  <th style={{ width: '90px' }} className="align-middle">Tiết / Buổi</th>
                  {CAC_THU_MA_TRAN.map((thu) => (
                    <th key={thu.id} style={{ minWidth: '135px' }} className="align-middle">
                      {thu.ten}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {/* Buổi Sáng: Tiết 1 - 6 */}
                <tr className="table-secondary py-1">
                  <td colSpan={8} className="fw-semibold small py-1 px-2 text-uppercase text-secondary">
                    Buổi sáng (Tiết 1 - 6)
                  </td>
                </tr>
                {CAC_TIET_MA_TRAN.slice(0, 6).map((tiet) => (
                  <tr key={tiet}>
                    <td className="text-center table-light fw-semibold small">
                      Tiết {tiet}
                    </td>
                    {CAC_THU_MA_TRAN.map((thu) => renderCellMaTran(thu.id, tiet))}
                  </tr>
                ))}

                {/* Buổi Chiều: Tiết 7 - 12 */}
                <tr className="table-secondary py-1">
                  <td colSpan={8} className="fw-semibold small py-1 px-2 text-uppercase text-secondary">
                    Buổi chiều (Tiết 7 - 12)
                  </td>
                </tr>
                {CAC_TIET_MA_TRAN.slice(6, 12).map((tiet) => (
                  <tr key={tiet}>
                    <td className="text-center table-light fw-semibold small">
                      Tiết {tiet}
                    </td>
                    {CAC_THU_MA_TRAN.map((thu) => renderCellMaTran(thu.id, tiet))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {/* MODAL CẢNH BÁO VI PHẠM RÀNG BUỘC KHI PHÂN CÔNG THỦ CÔNG */}
      {modalCanhBao.hienThi && (
        <div
          className="modal fade show d-block"
          tabIndex={-1}
          style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}
        >
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content">
              <div className="modal-header py-2 bg-light">
                <h5 className="modal-title h6 text-danger fw-bold">
                  Cảnh báo vi phạm ràng buộc
                </h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setModalCanhBao((prev) => ({ ...prev, hienThi: false }))}
                  disabled={dangXuLy}
                />
              </div>
              <div className="modal-body py-3">
                <p className="mb-2">
                  Phân công lớp <strong>{modalCanhBao.tenLop}</strong> cho giảng viên{' '}
                  <strong>{modalCanhBao.tenGiangVien}</strong> bị phát hiện vi phạm:
                </p>
                <div className="alert alert-warning py-2 mb-3 small">
                  <ul className="mb-0 ps-3">
                    {modalCanhBao.danhSachViPham.map((vp, i) => (
                      <li key={i}>{vp}</li>
                    ))}
                  </ul>
                </div>
                <p className="mb-0 text-muted small">
                  Bạn có chắc chắn muốn bỏ qua cảnh báo và ép gán phân công này không?
                </p>
              </div>
              <div className="modal-footer py-2">
                <button
                  type="button"
                  className="btn btn-sm btn-secondary"
                  onClick={() => setModalCanhBao((prev) => ({ ...prev, hienThi: false }))}
                  disabled={dangXuLy}
                >
                  Hủy bỏ
                </button>
                <button
                  type="button"
                  className="btn btn-sm btn-danger"
                  onClick={() => {
                    const lop = bangPhanCong?.danhSachLop.find(
                      (l) => l.maLopHocPhan === modalCanhBao.maLopHocPhan
                    )
                    if (lop) {
                      handleChonGiangVien(lop, modalCanhBao.maGiangVien, true)
                    }
                  }}
                  disabled={dangXuLy}
                >
                  {dangXuLy ? 'Đang lưu...' : 'Xác nhận ép gán'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}

export default KetQuaXepLich
