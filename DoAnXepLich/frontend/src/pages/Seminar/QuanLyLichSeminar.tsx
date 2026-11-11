import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { hocKyApi } from '../../api/hocKyApi'
import { boMonApi } from '../../api/boMonApi'
import { giangVienApi } from '../../api/giangVienApi'
import { nguyenVongApi } from '../../api/nguyenVongApi'
import { useAuth } from '../../contexts/AuthContext'
import Loading from '../../components/Loading'
import type { HocKy } from '../../types/hocKy'
import type { BoMon } from '../../types/boMon'
import type { GiangVien } from '../../types/giangVien'
import type { KetQuaTaoLichSeminar, LichBan } from '../../types/nguyenVong'

const CAC_THU = [
  { id: 2, ten: 'Thứ 2' },
  { id: 3, ten: 'Thứ 3' },
  { id: 4, ten: 'Thứ 4' },
  { id: 5, ten: 'Thứ 5' },
  { id: 6, ten: 'Thứ 6' },
  { id: 7, ten: 'Thứ 7' },
  { id: 8, ten: 'Chủ nhật' },
]

const CA_NHANH = [
  { nhan: 'Sáng (Tiết 1-3)', tbd: 1, tkt: 3 },
  { nhan: 'Sáng (Tiết 4-6)', tbd: 4, tkt: 6 },
  { nhan: 'Cả sáng (Tiết 1-6)', tbd: 1, tkt: 6 },
  { nhan: 'Chiều (Tiết 7-9)', tbd: 7, tkt: 9 },
  { nhan: 'Chiều (Tiết 10-12)', tbd: 10, tkt: 12 },
]

interface NhomSeminar {
  key: string
  tenSeminar: string
  thu: number
  tietBatDau: number
  tietKetThuc: number
  items: LichBan[]
}

function tenThuHienThi(thu: number) {
  return thu === 8 ? 'Chủ nhật' : `Thứ ${thu}`
}

function QuanLyLichSeminar() {
  const { user } = useAuth()
  const laTruongBoMon = user?.vaiTro === 'TruongBoMon' || user?.vaiTro === 'Admin'
  const laGiangVien = user?.vaiTro === 'GiangVien'

  const [danhSachHocKy, setDanhSachHocKy] = useState<HocKy[]>([])
  const [danhSachBoMon, setDanhSachBoMon] = useState<BoMon[]>([])
  const [danhSachGiangVien, setDanhSachGiangVien] = useState<GiangVien[]>([])

  const [maHocKy, setMaHocKy] = useState<number>(0)
  const [maBoMon, setMaBoMon] = useState<number>(user?.maBoMon || 0)
  const [dsLichBan, setDsLichBan] = useState<LichBan[]>([])

  // Form tạo lịch Seminar mới
  const [tenSeminar, setTenSeminar] = useState<string>('Seminar Bộ môn')
  const [thu, setThu] = useState<number>(3)
  const [tietBatDau, setTietBatDau] = useState<number>(4)
  const [tietKetThuc, setTietKetThuc] = useState<number>(6)
  const [tuKhoaGv, setTuKhoaGv] = useState<string>('')
  const [dsGvDaChon, setDsGvDaChon] = useState<number[]>([])

  const [dangTai, setDangTai] = useState<boolean>(false)
  const [dangLuu, setDangLuu] = useState<boolean>(false)
  const [ketQuaTao, setKetQuaTao] = useState<KetQuaTaoLichSeminar | null>(null)
  const [thongBao, setThongBao] = useState<string>('')
  const [loi, setLoi] = useState<string>('')

  // 1. Tải danh mục khởi tạo
  useEffect(() => {
    const khoiTao = async () => {
      try {
        setDangTai(true)
        const [resHK, resBM, resGV] = await Promise.all([
          hocKyApi.getAll(),
          boMonApi.getAll(),
          giangVienApi.getAll(),
        ])
        setDanhSachHocKy(resHK.data)
        setDanhSachBoMon(resBM.data)
        setDanhSachGiangVien(resGV.data)

        if (resHK.data.length > 0) {
          const hkMo = resHK.data.find((h) => h.trangThai === 'DangMo') ?? resHK.data[0]
          setMaHocKy(hkMo.maHocKy)
        }
      } catch {
        setLoi('Không thể tải danh mục học kỳ, bộ môn hoặc giảng viên.')
      } finally {
        setDangTai(false)
      }
    }
    void khoiTao()
  }, [])

  // 2. Tải danh sách lịch khóa cứng (Seminar + lịch bận khác)
  const taiDanhSachLichBan = async (hkId: number, bmId?: number) => {
    if (!hkId) return
    try {
      setDangTai(true)
      const res = await nguyenVongApi.getLichBan(
        hkId,
        undefined,
        bmId && bmId > 0 ? bmId : undefined
      )
      setDsLichBan(res.data)
    } catch {
      setLoi('Không thể tải danh sách lịch Seminar.')
    } finally {
      setDangTai(false)
    }
  }

  useEffect(() => {
    if (maHocKy > 0) {
      void taiDanhSachLichBan(maHocKy, maBoMon)
    }
  }, [maHocKy, maBoMon])

  // Tách lịch Seminar và lịch bận đã duyệt khác
  const dsSeminar = useMemo(
    () => dsLichBan.filter((x) => x.laSeminar || (x.lyDo || '').toLowerCase().includes('seminar')),
    [dsLichBan]
  )
  const dsLichBanKhac = useMemo(
    () => dsLichBan.filter((x) => !(x.laSeminar || (x.lyDo || '').toLowerCase().includes('seminar'))),
    [dsLichBan]
  )

  // Gom nhóm lịch Seminar theo Tên + Thứ + Tiết
  const cacNhomSeminar = useMemo<NhomSeminar[]>(() => {
    const map = new Map<string, NhomSeminar>()
    for (const item of dsSeminar) {
      const lyDoNorm = (item.lyDo || '[Seminar] Seminar Bộ môn').trim()
      const key = `${item.thu}_${item.tietBatDau}_${item.tietKetThuc}_${lyDoNorm.toLowerCase()}`
      let nhom = map.get(key)
      if (!nhom) {
        nhom = {
          key,
          tenSeminar: lyDoNorm,
          thu: item.thu,
          tietBatDau: item.tietBatDau,
          tietKetThuc: item.tietKetThuc,
          items: [],
        }
        map.set(key, nhom)
      }
      nhom.items.push(item)
    }
    return Array.from(map.values()).sort(
      (a, b) => a.thu - b.thu || a.tietBatDau - b.tietBatDau || a.tenSeminar.localeCompare(b.tenSeminar)
    )
  }, [dsSeminar])

  // Danh sách giảng viên đang làm việc, lọc theo bộ môn & từ khóa tìm kiếm
  const dsGvTrongBoMon = useMemo(
    () =>
      danhSachGiangVien.filter(
        (gv) =>
          gv.trangThai !== 'NghiViec' &&
          (maBoMon <= 0 || gv.maBoMon === maBoMon)
      ),
    [danhSachGiangVien, maBoMon]
  )

  const dsGvHienThiTrongForm = useMemo(() => {
    const kw = tuKhoaGv.trim().toLowerCase()
    if (!kw) return dsGvTrongBoMon
    return dsGvTrongBoMon.filter(
      (gv) =>
        gv.hoTen.toLowerCase().includes(kw) ||
        gv.email.toLowerCase().includes(kw) ||
        (gv.tenBoMon || '').toLowerCase().includes(kw)
    )
  }, [dsGvTrongBoMon, tuKhoaGv])

  // Tập các giảng viên đã có lịch Seminar trùng khung giờ đang nhập trên form
  const gvDaKhoaTrungKhungGioForm = useMemo(() => {
    const set = new Set<number>()
    for (const item of dsSeminar) {
      if (
        item.thu === thu &&
        Math.max(item.tietBatDau, tietBatDau) <= Math.min(item.tietKetThuc, tietKetThuc)
      ) {
        set.add(item.maGiangVien)
      }
    }
    return set
  }, [dsSeminar, thu, tietBatDau, tietKetThuc])

  // Đổi bộ môn -> cập nhật gợi ý tên Seminar & bỏ chọn các GV không thuộc bộ môn mới
  const handleDoiBoMon = (bmId: number) => {
    setMaBoMon(bmId)
    setDsGvDaChon([])
    if (bmId > 0) {
      const bm = danhSachBoMon.find((b) => b.maBoMon === bmId)
      if (bm && (tenSeminar === 'Seminar Bộ môn' || tenSeminar.startsWith('Seminar '))) {
        setTenSeminar(`Seminar ${bm.tenBoMon}`)
      }
    } else if (tenSeminar.startsWith('Seminar ')) {
      setTenSeminar('Seminar Bộ môn')
    }
  }

  const toggleChonGv = (maGv: number) => {
    setDsGvDaChon((prev) =>
      prev.includes(maGv) ? prev.filter((id) => id !== maGv) : [...prev, maGv]
    )
  }

  const handleChonTatCaHienThi = () => {
    const idsHienThi = dsGvHienThiTrongForm.map((g) => g.maGiangVien)
    setDsGvDaChon((prev) => Array.from(new Set([...prev, ...idsHienThi])))
  }

  const handleBoChonHet = () => {
    setDsGvDaChon([])
  }

  // Tạo lịch Seminar khóa cứng
  const handleTaoSeminar = async (e: FormEvent) => {
    e.preventDefault()
    setLoi('')
    setThongBao('')
    setKetQuaTao(null)

    if (!maHocKy || maHocKy <= 0) {
      setLoi('Vui lòng chọn học kỳ.')
      return
    }
    if (tietBatDau > tietKetThuc) {
      setLoi('Tiết bắt đầu không được lớn hơn tiết kết thúc.')
      return
    }
    if (dsGvDaChon.length === 0) {
      setLoi('Vui lòng tích chọn ít nhất 1 giảng viên tham gia Seminar.')
      return
    }

    try {
      setDangLuu(true)
      const res = await nguyenVongApi.taoLichSeminar({
        maHocKy,
        tenSeminar: tenSeminar.trim() || 'Seminar Bộ môn',
        thu,
        tietBatDau,
        tietKetThuc,
        danhSachMaGiangVien: dsGvDaChon,
      })
      setKetQuaTao(res.data)
      setThongBao(res.data.thongDiep)
      await taiDanhSachLichBan(maHocKy, maBoMon)
    } catch {
      setLoi('Không thể lưu lịch Seminar. Vui lòng kiểm tra lại dữ liệu.')
    } finally {
      setDangLuu(false)
    }
  }

  // Gỡ 1 giảng viên khỏi lịch Seminar (hoặc lịch bận cố định khác)
  const handleXoaMotBanGhi = async (item: LichBan) => {
    const tenGv = item.tenGiangVien || `GV #${item.maGiangVien}`
    if (
      !window.confirm(
        `Gỡ lịch "${item.lyDo || 'Lịch bận'}" vào ${tenThuHienThi(item.thu)} (Tiết ${item.tietBatDau}-${item.tietKetThuc}) của ${tenGv}?`
      )
    ) {
      return
    }

    try {
      setLoi('')
      setThongBao('')
      await nguyenVongApi.xoaLichBan(item.maLichBan)
      setThongBao(`Đã gỡ lịch của ${tenGv}.`)
      await taiDanhSachLichBan(maHocKy, maBoMon)
    } catch {
      setLoi('Không thể gỡ lịch này.')
    }
  }

  // Xóa cả nhóm Seminar
  const handleXoaNhomSeminar = async (nhom: NhomSeminar) => {
    if (
      !window.confirm(
        `Bạn có chắc chắn muốn xóa toàn bộ lịch "${nhom.tenSeminar}" (${tenThuHienThi(nhom.thu)}, Tiết ${nhom.tietBatDau}-${nhom.tietKetThuc}) cho tất cả ${nhom.items.length} giảng viên đang hiển thị?`
      )
    ) {
      return
    }

    try {
      setLoi('')
      setThongBao('')
      setKetQuaTao(null)
      const res = await nguyenVongApi.xoaNhomSeminar({
        maHocKy,
        thu: nhom.thu,
        tietBatDau: nhom.tietBatDau,
        tietKetThuc: nhom.tietKetThuc,
        lyDo: nhom.tenSeminar,
        maBoMon: maBoMon > 0 ? maBoMon : undefined,
      })
      setThongBao(res.data.message)
      await taiDanhSachLichBan(maHocKy, maBoMon)
    } catch {
      setLoi('Lỗi khi xóa nhóm lịch Seminar.')
    }
  }

  return (
    <div className="container-fluid py-2">
      {/* Header */}
      <div className="mb-3 d-flex flex-wrap justify-content-between align-items-start gap-2">
        <div>
          <h2 className="h4 mb-1 text-dark">
            Quản lý Lịch Seminar Khoa / Bộ môn
          </h2>
          <p className="text-muted small mb-0">
            Thiết lập lịch Seminar cố định theo học kỳ để hệ thống không xếp lịch giảng dạy trùng vào các khung giờ này.
          </p>
        </div>
      </div>

      {/* Thanh lọc Học kỳ & Bộ môn */}
      <div className="row g-2 mb-3 align-items-center bg-light p-2 border rounded">
        <div className="col-auto">
          <label className="col-form-label col-form-label-sm fw-semibold">Học kỳ:</label>
        </div>
        <div className="col-md-3">
          <select
            className="form-select form-select-sm"
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

        <div className="col-auto ms-md-2">
          <label className="col-form-label col-form-label-sm fw-semibold">Bộ môn:</label>
        </div>
        <div className="col-md-3">
          <select
            className="form-select form-select-sm"
            value={maBoMon}
            onChange={(e) => handleDoiBoMon(Number(e.target.value))}
          >
            <option value={0}>-- Tất cả bộ môn --</option>
            {danhSachBoMon.map((bm) => (
              <option key={bm.maBoMon} value={bm.maBoMon}>
                {bm.tenBoMon}
              </option>
            ))}
          </select>
        </div>

        <div className="col text-end small text-muted">
          Buổi Seminar: <strong className="text-primary">{cacNhomSeminar.length}</strong> | Lượt GV tham gia:{' '}
          <strong className="text-dark">{dsSeminar.length}</strong>
          {dsLichBanKhac.length > 0 && (
            <>
              {' '}
              | Lịch bận cố định khác: <strong className="text-secondary">{dsLichBanKhac.length}</strong>
            </>
          )}
        </div>
      </div>

      {/* Thông báo các lớp vừa bị tự động gỡ do trùng giờ Seminar */}
      {ketQuaTao && ketQuaTao.soLopBiGo > 0 && (
        <div className="alert alert-warning border-warning shadow-sm py-2 px-3 mb-3 small">
          <div className="fw-bold text-dark mb-1">
            Đã tự động gỡ phân công {ketQuaTao.soLopBiGo} lớp học phần bị trùng giờ Seminar:
          </div>
          <ul className="mb-0 ps-3">
            {ketQuaTao.danhSachLopBiGo.map((dong, idx) => (
              <li key={idx} className="text-danger fw-semibold">
                {dong}
              </li>
            ))}
          </ul>
        </div>
      )}

      {thongBao && (
        <div className="alert alert-success py-2 small mb-3">
          <strong>Thành công:</strong> {thongBao}
        </div>
      )}

      {loi && (
        <div className="alert alert-danger py-2 small mb-3">
          <strong>Lỗi:</strong> {loi}
        </div>
      )}

      {/* Form thiết lập lịch Seminar mới (Dành cho Trưởng bộ môn / Admin) */}
      {laTruongBoMon && (
        <div className="card border shadow-sm mb-4">
          <div className="card-header bg-white py-2 d-flex justify-content-between align-items-center">
            <span className="fw-bold text-dark">
              Thêm lịch Seminar mới
            </span>
            <span className="small text-muted">
              Tự động gỡ phân công các lớp trùng giờ khi lưu
            </span>
          </div>
          <div className="card-body">
            <form onSubmit={handleTaoSeminar}>
              <div className="row g-2 mb-3">
                <div className="col-md-4">
                  <label className="form-label small fw-semibold">
                    Tên buổi Seminar <span className="text-danger">*</span>
                  </label>
                  <input
                    type="text"
                    className="form-control form-control-sm"
                    placeholder="VD: Seminar Bộ môn Toán, Sinh hoạt học thuật..."
                    value={tenSeminar}
                    onChange={(e) => setTenSeminar(e.target.value)}
                    required
                  />
                </div>

                <div className="col-md-2">
                  <label className="form-label small fw-semibold">Thứ trong tuần</label>
                  <select
                    className="form-select form-select-sm"
                    value={thu}
                    onChange={(e) => setThu(Number(e.target.value))}
                  >
                    {CAC_THU.map((t) => (
                      <option key={t.id} value={t.id}>
                        {t.ten}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="col-md-2">
                  <label className="form-label small fw-semibold">Tiết bắt đầu</label>
                  <select
                    className="form-select form-select-sm"
                    value={tietBatDau}
                    onChange={(e) => {
                      const v = Number(e.target.value)
                      setTietBatDau(v)
                      if (tietKetThuc < v) setTietKetThuc(v)
                    }}
                  >
                    {Array.from({ length: 15 }, (_, i) => i + 1).map((t) => (
                      <option key={t} value={t}>
                        Tiết {t}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="col-md-2">
                  <label className="form-label small fw-semibold">Tiết kết thúc</label>
                  <select
                    className="form-select form-select-sm"
                    value={tietKetThuc}
                    onChange={(e) => setTietKetThuc(Number(e.target.value))}
                  >
                    {Array.from({ length: 15 }, (_, i) => i + 1).map((t) => (
                      <option key={t} value={t}>
                        Tiết {t}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="col-md-2 d-flex align-items-end">
                  <button
                    type="submit"
                    className="btn btn-primary btn-sm w-100 fw-semibold"
                    disabled={dangLuu || dsGvDaChon.length === 0}
                  >
                    {dangLuu ? 'Đang lưu...' : `Lưu lịch (${dsGvDaChon.length} GV)`}
                  </button>
                </div>
              </div>

              {/* Chọn nhanh khung tiết */}
              <div className="d-flex flex-wrap align-items-center gap-2 mb-3">
                <span className="small text-muted fw-semibold">Chọn nhanh khung tiết:</span>
                {CA_NHANH.map((ca) => {
                  const active = tietBatDau === ca.tbd && tietKetThuc === ca.tkt
                  return (
                    <button
                      key={ca.nhan}
                      type="button"
                      className={`btn btn-sm py-0 px-2 ${active ? 'btn-dark' : 'btn-outline-secondary'}`}
                      onClick={() => {
                        setTietBatDau(ca.tbd)
                        setTietKetThuc(ca.tkt)
                      }}
                    >
                      {ca.nhan}
                    </button>
                  )
                })}
              </div>

              {/* Chọn hàng loạt giảng viên tham gia */}
              <div className="border rounded p-2 bg-light">
                <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-2">
                  <div className="small fw-semibold text-dark">
                    Chọn giảng viên tham gia Seminar (Đã chọn:{' '}
                    <span className="text-primary">{dsGvDaChon.length}</span> / {dsGvTrongBoMon.length} GV)
                  </div>
                  <div className="d-flex flex-wrap align-items-center gap-2">
                    <input
                      type="text"
                      className="form-control form-control-sm"
                      style={{ width: '210px' }}
                      placeholder="Tìm tên, email GV..."
                      value={tuKhoaGv}
                      onChange={(e) => setTuKhoaGv(e.target.value)}
                    />
                    <button
                      type="button"
                      className="btn btn-outline-primary btn-sm py-0 px-2"
                      onClick={handleChonTatCaHienThi}
                      disabled={dsGvHienThiTrongForm.length === 0}
                    >
                      Chọn tất cả ({dsGvHienThiTrongForm.length})
                    </button>
                    <button
                      type="button"
                      className="btn btn-outline-secondary btn-sm py-0 px-2"
                      onClick={handleBoChonHet}
                      disabled={dsGvDaChon.length === 0}
                    >
                      Bỏ chọn hết
                    </button>
                  </div>
                </div>

                {dsGvHienThiTrongForm.length === 0 ? (
                  <div className="text-center text-muted small py-3">
                    Không tìm thấy giảng viên phù hợp trong bộ môn đã chọn.
                  </div>
                ) : (
                  <div
                    className="row g-2 overflow-auto"
                    style={{ maxHeight: '250px' }}
                  >
                    {dsGvHienThiTrongForm.map((gv) => {
                      const daChon = dsGvDaChon.includes(gv.maGiangVien)
                      const daKhoaTrungGio = gvDaKhoaTrungKhungGioForm.has(gv.maGiangVien)
                      return (
                        <div key={gv.maGiangVien} className="col-md-4 col-sm-6">
                          <label
                            className={`d-flex align-items-start gap-2 p-2 rounded border h-100 ${
                              daChon
                                ? 'border-primary bg-white shadow-sm'
                                : 'border-secondary-subtle bg-white'
                            }`}
                            style={{ cursor: 'pointer', fontSize: '13px' }}
                          >
                            <input
                              type="checkbox"
                              className="form-check-input mt-1"
                              checked={daChon}
                              onChange={() => toggleChonGv(gv.maGiangVien)}
                            />
                            <div className="flex-grow-1 lh-sm">
                              <div className="fw-semibold text-dark">
                                <span className="text-muted font-monospace small me-1">
                                  #{gv.maGiangVien}
                                </span>
                                {gv.hoTen}
                              </div>
                              <div className="text-muted" style={{ fontSize: '11px' }}>
                                {gv.tenBoMon || 'Chưa phân bộ môn'}
                                {gv.chucDanh ? ` • ${gv.chucDanh}` : ''}
                              </div>
                              {daKhoaTrungGio && (
                                <span
                                  className="badge bg-warning text-dark mt-1"
                                  style={{ fontSize: '10px' }}
                                >
                                  Đã có Seminar trùng khung giờ này
                                </span>
                              )}
                            </div>
                          </label>
                        </div>
                      )
                    })}
                  </div>
                )}
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Danh sách các nhóm Lịch Seminar */}
      <div className="card border bg-white shadow-sm mb-4">
        <div className="card-header bg-light py-2 d-flex justify-content-between align-items-center">
          <div className="fw-bold text-dark">
            Danh sách Lịch Seminar ({cacNhomSeminar.length} buổi)
            {maBoMon > 0 && (
              <span className="badge bg-primary ms-2">
                {danhSachBoMon.find((b) => b.maBoMon === maBoMon)?.tenBoMon}
              </span>
            )}
          </div>
          {laGiangVien && (
            <span className="small text-muted">
              Các khung giờ dưới đây sẽ không xếp lịch giảng dạy
            </span>
          )}
        </div>

        <div className="card-body">
          {dangTai ? (
            <Loading />
          ) : cacNhomSeminar.length === 0 ? (
            <div className="text-center text-muted py-4 small">
              Chưa có lịch Seminar nào cho bộ môn / học kỳ đang chọn.
            </div>
          ) : (
            <div className="d-flex flex-column gap-3">
              {cacNhomSeminar.map((nhom) => {
                const toiCoThamGia =
                  laGiangVien &&
                  user?.maGiangVien &&
                  nhom.items.some((x) => x.maGiangVien === user.maGiangVien)

                return (
                  <div
                    key={nhom.key}
                    className={`border rounded p-3 ${
                      toiCoThamGia ? 'border-warning bg-warning-subtle' : 'bg-light'
                    }`}
                  >
                    <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-2 pb-2 border-bottom">
                      <div className="d-flex flex-wrap align-items-center gap-2">
                        <span className="fw-bold text-dark fs-6">{nhom.tenSeminar}</span>
                        <span className="badge bg-dark">
                          {tenThuHienThi(nhom.thu)} • Tiết {nhom.tietBatDau} - {nhom.tietKetThuc}
                        </span>
                        <span className="badge bg-secondary">{nhom.items.length} giảng viên</span>
                        {toiCoThamGia && (
                          <span className="badge bg-warning text-dark">Bạn có lịch Seminar này</span>
                        )}
                      </div>

                      {laTruongBoMon && (
                        <button
                          type="button"
                          className="btn btn-outline-danger btn-sm py-0 px-2"
                          onClick={() => void handleXoaNhomSeminar(nhom)}
                        >
                          Xóa cả nhóm Seminar
                        </button>
                      )}
                    </div>

                    <div className="d-flex flex-wrap gap-2">
                      {nhom.items.map((item) => {
                        const laBanThan =
                          laGiangVien && user?.maGiangVien === item.maGiangVien
                        return (
                          <span
                            key={item.maLichBan}
                            className={`badge d-inline-flex align-items-center gap-1 py-1 px-2 border ${
                              laBanThan
                                ? 'bg-warning text-dark border-dark'
                                : 'bg-white text-dark border-secondary-subtle'
                            }`}
                            style={{ fontSize: '12px', fontWeight: 500 }}
                          >
                            <span className="font-monospace text-muted">
                              #{item.maGiangVien}
                            </span>
                            <span className="fw-semibold">{item.tenGiangVien}</span>
                            {item.tenBoMon && (
                              <span className="text-muted">({item.tenBoMon})</span>
                            )}
                            {laBanThan && <strong className="ms-1">(Bạn)</strong>}
                            {laTruongBoMon && (
                              <button
                                type="button"
                                className="btn-close ms-1"
                                style={{ fontSize: '9px' }}
                                title={`Gỡ ${item.tenGiangVien} khỏi lịch Seminar này`}
                                aria-label="Gỡ giảng viên"
                                onClick={() => void handleXoaMotBanGhi(item)}
                              />
                            )}
                          </span>
                        )
                      })}
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </div>
      </div>

      {/* Bảng phụ: Lịch bận cố định khác từ đơn xin nghỉ đã duyệt */}
      {dsLichBanKhac.length > 0 && (
        <div className="card border bg-white">
          <div className="card-header bg-light py-2 fw-semibold text-secondary small">
            Lịch bận cố định khác trong học kỳ ({dsLichBanKhac.length})
          </div>
          <div className="table-responsive">
            <table className="table table-bordered table-sm table-hover mb-0 small">
              <thead className="table-light text-center">
                <tr>
                  <th style={{ width: '45px' }}>STT</th>
                  <th>Giảng viên</th>
                  <th>Bộ môn</th>
                  <th>Khung giờ</th>
                  <th>Lý do</th>
                  {laTruongBoMon && <th style={{ width: '80px' }}>Thao tác</th>}
                </tr>
              </thead>
              <tbody>
                {dsLichBanKhac.map((item, idx) => (
                  <tr key={item.maLichBan} className="align-middle">
                    <td className="text-center">{idx + 1}</td>
                    <td className="fw-semibold">
                      #{item.maGiangVien} {item.tenGiangVien}
                    </td>
                    <td className="text-center">{item.tenBoMon || '-'}</td>
                    <td className="text-center">
                      {tenThuHienThi(item.thu)} • Tiết {item.tietBatDau} - {item.tietKetThuc}
                    </td>
                    <td>{item.lyDo || 'Lịch bận cố định'}</td>
                    {laTruongBoMon && (
                      <td className="text-center">
                        <button
                          type="button"
                          className="btn btn-outline-danger btn-sm py-0 px-2"
                          onClick={() => void handleXoaMotBanGhi(item)}
                        >
                          Gỡ
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}

export default QuanLyLichSeminar
